using DatabaseMod.Alterations;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using FileMod;
using LoginApi;
using LoginMod;
using LogMod;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqliteMod;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApiApp;

internal class Program {
    private static void MapEndpoints(WebApplication app) {
        // Login
        app.MapPost("/api/login", LoginEndpoints.Login).AllowAnonymous();
        app.MapPost("/api/logout", LoginEndpoints.Logout).AllowAnonymous();
        app.MapPost("/api/register", LoginEndpoints.Register).AllowAnonymous();
        app.MapGet("/api/login-info", LoginEndpoints.LoginInfo).AllowAnonymous();

        // Files
        app.MapPost("/api/create-file", FileEndpoints.CreateFile).RequireAuthorization("Admin");
        app.MapPost("/api/create-folder", FileEndpoints.CreateFolder).RequireAuthorization("Admin");
        app.MapPost("/api/delete-file", FileEndpoints.DeleteFile).RequireAuthorization("Admin");
        app.MapGet("/api/download-file", FileEndpoints.DownloadFile).RequireAuthorization("Admin");
        app.MapGet("/api/get-file-node", FileEndpoints.GetFileNode).RequireAuthorization("Admin");
        app.MapPost("/api/move-file", FileEndpoints.MoveFile).RequireAuthorization("Admin");
        app.MapPost("/api/upload-files", FileEndpoints.UploadFiles).RequireAuthorization("Admin");

        // Database
        app.MapGet("/api/get-database", DatabaseEndpoints.GetDatabase).RequireAuthorization("Admin");
        app.MapPost("/api/alter-database", DatabaseEndpoints.AlterDatabase).RequireAuthorization("Admin");
        app.MapPost("/api/create-table-based-on-file-node", DatabaseEndpoints.CreateTableBasedOnFileNode).RequireAuthorization("Admin");

        // Database records
        app.MapPut("/api/select-records", RecordEndpoints.SelectRecords).RequireAuthorization("Admin");
        app.MapPost("/api/insert-records", RecordEndpoints.InsertRecords).RequireAuthorization("Admin");
        app.MapPost("/api/update-records", RecordEndpoints.UpdateRecords).RequireAuthorization("Admin");
        app.MapPost("/api/delete-records", RecordEndpoints.DeleteRecords).RequireAuthorization("Admin");
    }

    private static void Main(string[] args) {
        WebApplication app;
        {
            ConfigurationManager configuration;
            Dictionary<Type, SqliteConnectionStringBuilder> migratableDbContexts = new();
            bool enableReverseProxy;

            // Configure services
            {
                var builder = WebApplication.CreateBuilder(args);
                configuration = builder.Configuration;
                IServiceCollection serviceCollection = builder.Services;

                // Reverse proxy
                {
                    var reverseProxy = configuration.GetSection("ReverseProxy");
                    enableReverseProxy = reverseProxy.Exists();
                    if (enableReverseProxy) {
                        builder.Services.AddReverseProxy().LoadFromConfig(reverseProxy);
                    }
                }

                // Auth
                serviceCollection.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options => options.Events = new ApiFriendlyCookieAuthenticationEvents());
                serviceCollection.AddAuthorization(options => {
                    options.FallbackPolicy = options.DefaultPolicy;
                    options.AddPolicy("Admin", auth => auth.RequireRole("Admin"));
                    options.AddPolicy("Elevated", auth => auth.RequireRole("Admin").RequireClaim("Elevated"));
                });

                // Minimal API
                builder.Services.Configure<JsonOptions>(options => {
                    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.SerializerOptions.AllowTrailingCommas = true;
                    options.SerializerOptions.PropertyNameCaseInsensitive = true;
                    options.SerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                });

                // User files
                {
                    string configuredPath =
                        configuration.GetValue<string?>("App:UserFilesRoot")
                        ?? throw new Exception("Missing \"App:UserFilesRoot\" configuration.");
                    var userFilesRoot = Path.GetFullPath(configuredPath, builder.Environment.ContentRootPath);
                    Directory.CreateDirectory(userFilesRoot);
                    serviceCollection.AddSingleton(new UserFileProvider(userFilesRoot));
                }

                // Login
                {
                    var loginCSB = new SqliteConnectionStringBuilder(configuration.GetConnectionString("Main"));
                    loginCSB.DataSource = Path.GetFullPath(loginCSB.DataSource, builder.Environment.ContentRootPath);
                    string loginConnectionString = loginCSB.ConnectionString;

                    migratableDbContexts.Add(typeof(LoginDbContext), loginCSB);
                    serviceCollection.AddDbContext<LoginDbContext>(o => o.UseSqlite(loginConnectionString));
                    serviceCollection.AddScoped<LoginServices>();
                }

                //// Content
                //{
                //    var database = new Database<ContentContext>();
                //    database.ContributeQueryContext(typeof(ContentContext));
                //}

                app = builder.Build();
            }

            Log.Factory = app.Services.GetRequiredService<ILoggerFactory>();

            { // Migrate databases
                foreach (var group in migratableDbContexts.GroupBy(o => o.Value.ConnectionString)) {
                    var connectionStringBuilder = group.First().Value;
                    var dbContextTypes = group.Select(o => o.Key).ToList();

                    // Try to create the Sqlite database's directory if it doesn't exist
                    if (connectionStringBuilder.Mode != SqliteOpenMode.Memory &&
                        !File.Exists(connectionStringBuilder.DataSource)) {

                        var dir = Path.GetDirectoryName(connectionStringBuilder.DataSource);
                        if (string.IsNullOrWhiteSpace(dir)) {
                            throw new Exception("Invalid DataSource.");
                        }

                        try { Directory.CreateDirectory(dir); } catch (Exception) { }
                    }

                    using var connection = new SqliteConnection(connectionStringBuilder.ToString());
                    connection.Open();

                    var currentDatabase = new Database();
                    currentDatabase.ContributeSqlite(connection);

                    var goalDatabase = new Database();
                    dbContextTypes.ForEach(goalDatabase.ContributeQueryableContext);

                    var alterations = new List<DatabaseAlteration>();
                    foreach (var goalSchema in goalDatabase.Schemas) {
                        if (currentDatabase.Schemas.SingleOrDefault(o => o.Name == goalSchema.Name) is not Schema currentSchema) {
                            currentSchema = new Schema(goalSchema.Name) {
                                Owner = goalSchema.Owner,
                            };

                            alterations.Add(new CreateSchema(currentSchema.Name, currentSchema.Owner));
                        }
                        foreach (var goalTable in goalSchema.Tables) {
                            var currentTable = currentSchema.Tables.SingleOrDefault(o => o.Name == goalTable.Name);
                            var tableAlterations = TableDiffer.DiffTables(goalSchema.Name, currentTable, goalTable);

                            alterations.AddRange(tableAlterations);
                        }
                    }

                    var sqlStatements = SqliteDatabaseScripter.ScriptAlterations(alterations, suppressNotSupportedExceptions: true);
                    foreach (var sql in sqlStatements) {
                        connection.Execute(sql);
                    }
                }
            }

            // Configure the HTTP request pipeline.
            {
                app.UseHttpsRedirection();

                if (enableReverseProxy) {
                    app.MapReverseProxy();
                }

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                //app.MapControllers();

                MapEndpoints(app);
            }
        }

        app.Run();
    }

}
