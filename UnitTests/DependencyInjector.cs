using DataMod;
using DataMod.EF;
using LoginMod;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

internal static class DependencyInjector
{
    public static IServiceProvider CreateServiceProvider()
    {
        ServiceCollection serviceCollection = new();

        string connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = "UnitTests",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        serviceCollection.AddSingleton<PasswordHashing>();
        serviceCollection.AddDb<LoginDb>(o => o.UseSqlite(connectionString));
        serviceCollection.AddScoped<LoginServices>();

        ServiceProvider services = serviceCollection.BuildServiceProvider();

        var dbContext = services.GetRequiredService<ComponentDbContext<LoginDb>>();
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();

        return services;
    }

    public static IServiceScope CreateScope()
    {
        return CreateServiceProvider().CreateScope();
    }
}