using DatabaseMod.Alterations;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using DataCore;
using DataCore.Annotations;
using DataMod.Sqlite;
using Microsoft.Data.Sqlite;

namespace xUnitTests;

public sealed class UserContext {
    public static FromQuery<UserContext, Role> Roles => new();
    public static FromQuery<UserContext, User> Users => new();
    public static FromQuery<UserContext, UserRole> UserRoles => new();

    public static IReadOnlyDatabase<UserContext> Database { get; }
    static UserContext() {
        var database = new Database<UserContext>();
        database.ContributeQueryContext();
        Database = database;
    }

    private UserContext() { throw new InvalidOperationException("Static only"); }
}

public readonly record struct Role(Guid RoleId, string Name) {
    public Role(string name) : this(Guid.NewGuid(), name) {
        RoleId = Guid.NewGuid();
        Name = name;
    }
}

public readonly record struct User(Guid UserId, string Username) {
    public User(string username) : this(Guid.NewGuid(), username) {
        UserId = Guid.NewGuid();
        Username = username;
    }
}

[PrimaryKey(nameof(UserId), nameof(RoleId))]
public readonly record struct UserRole(Guid UserId, Guid RoleId) {
}

internal static class Shared {
    public static SqliteCommandComposer<UserContext> Composer { get; } = new(UserContext.Database);

    public static IReadOnlyDictionary<string, User> Users { get; } = new Dictionary<string, User>() {
        { "Alice", new("Alice") },
        { "George", new("George") },
        { "Jeremy", new("Jeremy") },
        { "Stefan", new("Stefan") },
    };

    public static IReadOnlyDictionary<string, Role> Roles { get; } = new Dictionary<string, Role>() {
        { "Admin", new("Admin") },
        { "Guest", new("Guest") },
        { "Staff", new("Staff") },
    };

    public static IReadOnlyList<UserRole> UserRoles { get; } = new List<UserRole>() {
        new(Users["Alice"].UserId, Roles["Admin"].RoleId),
        new(Users["George"].UserId, Roles["Guest"].RoleId),
        new(Users["Stefan"].UserId, Roles["Staff"].RoleId),
    };

    /// <summary>
    /// Creates an in-memory connection with some pre-created objects based on the <see cref="UserContext"/>.
    /// </summary>
    /// <returns></returns>
    public static SqliteConnection CreateConnection() {
        var connectionStringBuilder = new SqliteConnectionStringBuilder() {
            DataSource = "UnitTests" + Random.Shared.Next(),
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        };

        if (connectionStringBuilder.Mode != SqliteOpenMode.Memory) {
            Console.WriteLine("Test Database: " + connectionStringBuilder.DataSource);
            if (File.Exists(connectionStringBuilder.DataSource)) {
                File.Delete(connectionStringBuilder.DataSource);
            }
        }

        var connection = new SqliteConnection(connectionStringBuilder.ToString());
        connection.Open();

        var currentDatabase = new Database();
        var goalDatabase = UserContext.Database;

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

        SqliteQueryRunner<UserContext> runner = new(Composer, new StaticDbConnectionPool<UserContext, SqliteConnection>(connection));

        runner.Execute(UserContext
                .Roles
                .InsertRange(Roles.Values));

        runner.Execute(UserContext
                .Users
                .InsertRange(Users.Values));

        runner.Execute(UserContext
                .UserRoles
                .InsertRange(UserRoles));

        return connection;
    }
}
