using DatabaseMod.Alterations;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using LoginApi;
using LoginMod;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqliteMod;
using System.Data.Common;
using System.Text.Json.Serialization;
using System.Text.Json;
using WebApiApp;
using Microsoft.AspNetCore.Http.Json;

internal class Program {
    private static void Main(string[] args) {
        ConfigurationManager configuration;
        List<Type> dbContextTypes = new();
        SqliteConnectionStringBuilder connectionStringBuilder;
        IConfigurationSection reverseProxy;

        WebApplication app;

        // Configure services
        {
            var builder = WebApplication.CreateBuilder(args);
            configuration = builder.Configuration;
            var serviceCollection = builder.Services;

            // Add services to the container.

            // Reverse proxy
            reverseProxy = builder.Configuration.GetSection("ReverseProxy");
            if (reverseProxy.Exists()) {
                builder.Services.AddReverseProxy().LoadFromConfig(reverseProxy);
            }

            // OpenAPI https://aka.ms/aspnetcore/swashbuckle
            serviceCollection.AddEndpointsApiExplorer();
            serviceCollection.AddSwaggerGen();

            // Auth
            serviceCollection.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => options.Events = new ApiFriendlyCookieAuthenticationEvents());
            serviceCollection.AddAuthorization(options => {
                options.FallbackPolicy = options.DefaultPolicy;
                options.AddPolicy("Admin", auth => auth.RequireRole("Admin"));
            });

            // Minimal API
            builder.Services.Configure<JsonOptions>(options => {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.AllowTrailingCommas = true;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
                options.SerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            });

            // Connection string
            connectionStringBuilder = new SqliteConnectionStringBuilder(configuration.GetConnectionString("Main"));
            connectionStringBuilder.DataSource = Path.GetFullPath(connectionStringBuilder.DataSource, builder.Environment.ContentRootPath);
            serviceCollection.AddSingleton(connectionStringBuilder);

            // Database
            serviceCollection.AddScoped(svc => new SqliteConnection(svc.GetRequiredService<SqliteConnectionStringBuilder>().ConnectionString));
            serviceCollection.AddScoped<DbConnection>(svc => svc.GetRequiredService<SqliteConnection>());

            // Login
            {
                dbContextTypes.Add(typeof(LoginDbContext));
                serviceCollection.AddDbContext<LoginDbContext>(o => o.UseSqlite(connectionStringBuilder.ConnectionString));
                serviceCollection.AddScoped<LoginServices>();
            }

            //// Content
            //{
            //    var database = new Database<ContentContext>();
            //    database.ContributeQueryContext(typeof(ContentContext));
            //}

            app = builder.Build();
        }

        { // Migrate database

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

            var sqlStatements = SqliteDatabaseScripter.ScriptAlterations(alterations);
            foreach (var sql in sqlStatements) {
                connection.Execute(sql);
            }
        }

        // Configure the HTTP request pipeline.
        {
            app.UseHttpsRedirection();

            if (reverseProxy.Exists()) {
                app.MapReverseProxy();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Require authentication to access Swagger
            app.Use(async (context, next) => {
                var path = context.Request.Path;
                if (path.Value?.Contains("/swagger/", StringComparison.OrdinalIgnoreCase) == true) {
                    if (!context.User.Identity!.IsAuthenticated) {
                        context.Response.Redirect("/login");
                        return;
                    }
                }
                await next();
            });

            //app.MapControllers();

            // Login
            app.MapPost("/api/login", LoginEndpoints.Login).AllowAnonymous();
            app.MapPost("/api/logout", LoginEndpoints.Logout).AllowAnonymous();
            app.MapPost("/api/register", LoginEndpoints.Register).AllowAnonymous();
            app.MapGet("/api/login-info", LoginEndpoints.LoginInfo).AllowAnonymous();

            // Database
            app.MapGet("/api/database", DatabaseEndpoints.Database);
            app.MapPost("/api/alter-database", DatabaseEndpoints.AlterDatabase).RequireAuthorization("Admin");
        }

        app.Run();
    }
}
