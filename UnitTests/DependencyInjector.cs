﻿using DatabaseMod.Alterations;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using DataCore;
using DataMod.Sqlite;
using LoginMod;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace UnitTests;

internal static class DependencyInjector {
    internal static IServiceProvider Services => LazyServices.Value;

    internal static IServiceScope CreateScope() {
        return Services.CreateScope();
    }

    private static readonly Lazy<IServiceProvider> LazyServices = new(() => {
        ServiceCollection serviceCollection = new();

        //var connection = CreateConnection();

        //var loginDatabase = new Database<ILoginDb>();
        //loginDatabase.ContributeQueryContext(typeof(ILoginDb));
        //serviceCollection.AddSingleton<IReadOnlyDatabase<ILoginDb>>(loginDatabase);

        //// Login
        //serviceCollection.AddSingleton<PasswordHashing>();
        //serviceCollection.AddSingleton<DbConnectionStringBuilder>(connectionStringBuilder);
        //serviceCollection.AddSingleton<IDbConnectionString<ILoginDb>, DbConnectionString<ILoginDb>>();
        //serviceCollection.AddScoped<IQueryOrchestrator<ILoginDb>, SqliteQueryOrchestrator<ILoginDb>>();
        //serviceCollection.AddScoped<ILoginDb, LoginDb>();
        //serviceCollection.AddScoped<LoginServices>();

        ServiceProvider services = serviceCollection.BuildServiceProvider();
        return services;
    });

    /// <summary>
    /// Creates an in-memory connection.
    /// </summary>
    /// <returns></returns>
    public static SqliteConnection CreateConnection() {
        var connectionStringBuilder = new SqliteConnectionStringBuilder() {
            DataSource = "UnitTests" + Random.Shared.Next(),
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        };

        Console.WriteLine("Test Database: " + connectionStringBuilder.DataSource);

        // Migrate database
        if (File.Exists(connectionStringBuilder.DataSource)) {
            File.Delete(connectionStringBuilder.DataSource);
        }

        var connection = new SqliteConnection(connectionStringBuilder.ToString());
        connection.Open();

        var currentDatabase = new Database();

        var goalDatabase = new Database();
        goalDatabase.ContributeQueryContext(typeof(LoginDb));

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

        return connection;
    }
}
