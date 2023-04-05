using DatabaseMod.Alterations;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using DataCore.Annotations;
using DataCore;
using DataMod.Sqlite;
using Microsoft.Data.Sqlite;

namespace xUnitTests;

public sealed class LoginDb {
    public static LoginDb Instance { get; } = new();

    public IReadOnlyDatabase<LoginDb> Database { get; }
    public IQuery<LoginDb, LocalLogin> LocalLogin => new FromQuery<LoginDb, LocalLogin>();

    private LoginDb() {
        var database = new Database<LoginDb>();
        database.ContributeQueryContext();
        Database = database;
    }
}

[PrimaryKey(nameof(UserId))]
public readonly record struct LocalLogin(Guid UserId, int Version, string Username) {
    public LocalLogin(string username) : this(Guid.NewGuid(), 0, username) {
        UserId = Guid.NewGuid();
        Version = 0;
        Username = username;
    }
}

internal static class Shared {
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
        var goalDatabase = LoginDb.Instance.Database;

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
