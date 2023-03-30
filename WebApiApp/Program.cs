using ContentMod;
using ContentServer;
using DatabaseMod.Alterations;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using DataCore.EF;
using DataMod;
using DataMod.EF;
using DataMod.Sqlite;
using LoginApi;
using LoginMod;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private static async Task Main(string[] args)
    {
        WebApplication app;
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // OpenAPI https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Auth
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie(options =>
                {
                    options.Events = new ApiFriendlyCookieAuthenticationEvents();
                });
            builder.Services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);

            // Web API
            //builder.Services.AddMvc();

            // App-specific
            var csBuilder = new SqliteConnectionStringBuilder()
            {
                DataSource = Path.GetFullPath("../-development/NeonMuon.db", builder.Environment.ContentRootPath),
                //Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
            };
            if (csBuilder.Mode != SqliteOpenMode.Memory &&
                !File.Exists(csBuilder.DataSource))
            {
                var dir = Path.GetDirectoryName(csBuilder.DataSource);
                if (string.IsNullOrWhiteSpace(dir))
                {
                    throw new Exception("Invalid DataSource.");
                }
                Directory.CreateDirectory(dir);
            }

            builder.Services.AddSingleton<PasswordHashing>();
            builder.Services.AddDb<ILoginDb, LoginDb>(o => o.UseSqlite(csBuilder.ToString()));
            builder.Services.AddScoped<LoginServices>();

            builder.Services.AddDb<IContentDb, ContentDb>(o => o.UseSqlite(csBuilder.ToString()));

            app = builder.Build();
        }

        using (var scope = app.Services.CreateScope())
        {
            var dbContexts = scope.ServiceProvider.GetServices<IComponentDbContext>()
                .Cast<DbContext>()
                .ToList();

            var databaseGroups = dbContexts
                .GroupBy(o => (o.Database.GetDbConnection().DataSource, o.Database.GetDbConnection().Database));

            foreach (var databaseGroup in databaseGroups)
            {
                var currentDatabase = new Database();
                var goalDatabase = new Database();

                SqliteConnection? connection = default;

                foreach (var dbContext in databaseGroup)
                {
                    dbContext.Database.OpenConnection();
                    dbContext.Database.EnsureCreated();

                    connection ??= (SqliteConnection)dbContext.Database.GetDbConnection();

                    await currentDatabase.ContributeSqliteAsync(connection);

                    goalDatabase.ContributeEFCore(dbContext.GetService<IDesignTimeModel>().Model);
                }

                var alterations = new List<DatabaseAlteration>();
                foreach (var goalSchema in goalDatabase.Schemas)
                {
                    if (currentDatabase.Schemas.SingleOrDefault(o => o.Name == goalSchema.Name) is not Schema currentSchema)
                    {
                        currentSchema = new Schema(goalSchema.Name)
                        {
                            Owner = goalSchema.Owner,
                        };

                        alterations.Add(new CreateSchema(currentSchema.Name, currentSchema.Owner));
                    }
                    foreach (var goalTable in goalSchema.Tables)
                    {
                        var currentTable = currentSchema.Tables.SingleOrDefault(o => o.Name == goalTable.Name);
                        var tableAlterations = TableDiffer.DiffTables(goalSchema.Name, currentTable, goalTable);

                        alterations.AddRange(tableAlterations);
                    }
                }

                var sqlStatements = SqliteDatabaseScripter.ScriptAlterations(alterations);
                foreach (var sql in sqlStatements)
                {
                    await connection!.ExecuteAsync(sql);
                }
            }
        }

        // Configure the HTTP request pipeline.

        app.UseHttpsRedirection();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        // Require authentication to access Swagger
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (path.Value?.Contains("/swagger/", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (!context.User.Identity!.IsAuthenticated)
                {
                    context.Response.Redirect("/login");
                    return;
                }
            }
            await next();
        });

        //app.MapControllers();

        app.MapPost("/api/login", LoginController.Login).AllowAnonymous();
        app.MapPost("/api/register", LoginController.Register).AllowAnonymous();

        app.Run();
    }
}
