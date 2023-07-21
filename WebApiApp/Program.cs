using DatabaseMod.Alterations;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using FileMod;
using LoginApi;
using LoginMod;
using LogMod;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using SqliteMod;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApiApp;
using static WebApiApp.HooksEndpoints;

internal class Program {
    private static void MapEndpoints(WebApplication app) {
        // Hooks
        app.MapPost("/api/gh", HooksEndpoints.GitHub).AllowAnonymous();

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
            Dictionary<Type, string> migratableDbContexts = new();
            bool enableReverseProxy;

            // Configure services
            {
                var builder = WebApplication.CreateBuilder(args);

                var appSettings = builder.AddDataDirectory(basePath => new AppSettings(basePath));
                foreach (var source in builder.Configuration.Sources.OfType<JsonConfigurationSource>().ToArray()) {
                    builder.Configuration.AddJsonFile(appSettings.GetFullPath(), source.Optional, source.ReloadOnChange);
                }

                var appData = builder.AddDataDirectory(basePath => new AppData(basePath));
                var userData = builder.AddDataDirectory(basePath => new UserData(basePath));

                // GitHub webhook
                var githubSection = builder.Configuration.GetSection("GitHub");
                if (githubSection.Exists()) {
                    builder.Services.AddSingleton(githubSection.Get<GitHubOptions>()!);
                }

                // Reverse proxy
                {
                    var reverseProxy = builder.Configuration.GetSection("ReverseProxy");
                    enableReverseProxy = reverseProxy.Exists();
                    if (enableReverseProxy) {
                        builder.Services.AddReverseProxy().LoadFromConfig(reverseProxy);
                    }
                }

                // Data protection
                builder.AddDataProtection(appSettings, appData);

                // Auth
                builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options => options.Events = new ApiFriendlyCookieAuthenticationEvents());
                builder.Services.AddAuthorization(options => {
                    //options.FallbackPolicy = options.DefaultPolicy;
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

                // Login
                {
                    string loginConnectionString = appData.GetConnectionString("app.db");

                    migratableDbContexts.Add(typeof(LoginDbContext), loginConnectionString);

                    builder.Services.AddDbContext<LoginDbContext>(o => o.UseSqlite(loginConnectionString));
                    builder.Services.AddScoped<LoginServices>();
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
                foreach (var group in migratableDbContexts.GroupBy(o => o.Value)) {
                    var connectionString = group.First().Value;
                    var dbContextTypes = group.Select(o => o.Key).ToList();

                    using var connection = new SqliteConnection(connectionString);
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
                app.UseForwardedHeaders(new ForwardedHeadersOptions {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });

                if (app.Configuration.GetSection("ForceHttps").Get<bool>() == true) {
                    const string https = "https";
                    app.Use((context, next) => {
                        context.Request.Scheme = https;
                        return next(context);
                    });
                }

                app.UseHttpsRedirection();

                if (enableReverseProxy) {
                    app.MapReverseProxy();
                }

                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                //app.MapControllers();

                MapEndpoints(app);
                app.MapFallbackToFile("index.html");
            }
        }

        app.Run();
    }

}
