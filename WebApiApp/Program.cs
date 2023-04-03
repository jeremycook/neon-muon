using ContentMod;
using DatabaseMod.Alterations;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using DataCore;
using DataMod.Sqlite;
using LoginApi;
using LoginMod;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using System.Data.Common;

internal class Program {
    private static void Main(string[] args) {
        WebApplication app;

        // Configure services
        {
            var builder = WebApplication.CreateBuilder(args);
            var serviceCollection = builder.Services;

            // Add services to the container.

            // OpenAPI https://aka.ms/aspnetcore/swashbuckle
            serviceCollection.AddEndpointsApiExplorer();
            serviceCollection.AddSwaggerGen();

            // Auth
            serviceCollection.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie(options => {
                    options.Events = new ApiFriendlyCookieAuthenticationEvents();
                });
            serviceCollection.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);

            // Web API
            //serviceCollection.AddMvc();

            // App Services

            // Default connection string
            var connectionStringBuilder = new SqliteConnectionStringBuilder() {
                DataSource = Path.GetFullPath("../-development/NeonMuon.db", builder.Environment.ContentRootPath),
                //Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
            };
            serviceCollection.AddSingleton<DbConnectionStringBuilder>(connectionStringBuilder);

            // Migrate database
            if (connectionStringBuilder is SqliteConnectionStringBuilder sqliteConnectionStringBuilder) {

                if (sqliteConnectionStringBuilder.Mode != SqliteOpenMode.Memory &&
                    !File.Exists(sqliteConnectionStringBuilder.DataSource)) {
                    var dir = Path.GetDirectoryName(sqliteConnectionStringBuilder.DataSource);
                    if (string.IsNullOrWhiteSpace(dir)) {
                        throw new Exception("Invalid DataSource.");
                    }
                    Directory.CreateDirectory(dir);
                }

                using (var connection = new SqliteConnection(sqliteConnectionStringBuilder.ToString())) {
                    connection.Open();

                    var currentDatabase = new Database();
                    currentDatabase.ContributeSqlite(connection);

                    var goalDatabase = new Database();
                    goalDatabase.ContributeQueryContext(typeof(LoginDb));
                    goalDatabase.ContributeQueryContext(typeof(IContentDb));

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
            }


            // Login
            {
                var database = new Database<LoginDb>();
                database.ContributeQueryContext(typeof(LoginDb));
                serviceCollection.AddSingleton<IReadOnlyDatabase<LoginDb>>(database);

                serviceCollection.AddSingleton<PasswordHashing>();
                serviceCollection.AddSingleton<IDbConnectionString<LoginDb>, DbConnectionString<LoginDb>>();
                serviceCollection.AddScoped<IDbCommandComposer<LoginDb>, SqliteCommandComposer<LoginDb>>();
                serviceCollection.AddScoped<LoginDb, LoginDb>();
                serviceCollection.AddScoped<LoginServices>();
            }

            // Content
            {
                var database = new Database<IContentDb>();
                database.ContributeQueryContext(typeof(IContentDb));
                serviceCollection.AddSingleton<IReadOnlyDatabase<IContentDb>>(database);

                serviceCollection.AddSingleton<IDbConnectionString<IContentDb>, DbConnectionString<IContentDb>>();
                serviceCollection.AddScoped<IDbCommandComposer<IContentDb>, SqliteCommandComposer<IContentDb>>();
                serviceCollection.AddScoped<IContentDb, ContentDb>();
            }

            app = builder.Build();
        }

        // Configure the HTTP request pipeline.
        {
            app.UseHttpsRedirection();

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

            app.MapPost("/api/login", LoginController.Login).AllowAnonymous();
            app.MapPost("/api/register", LoginController.Register).AllowAnonymous();
        }

        app.Run();
    }
}
