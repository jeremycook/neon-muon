using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using Microsoft.Data.Sqlite;
using SqliteMod;

namespace WebApiApp;

public class DatabaseEndpoints {

    public static Database Database(
        SqliteConnection connection
    ) {
        connection.Open();

        var database = new Database();
        database.ContributeSqlite(connection);
        return database;
    }

    public static IResult AlterDatabase(
        SqliteConnection connection,
        DatabaseAlteration[] databaseAlterations
    ) {
        var validAlterations = new[] {
            typeof(CreateColumn),
            typeof(AlterColumn),
            typeof(RenameColumn),
            typeof(DropColumn),

            typeof(CreateTable),
            typeof(DropTable),
            typeof(RenameTable),
        };

        var invalidAlterations = databaseAlterations
            .Where(alt => !validAlterations.Contains(alt.GetType()));
        if (invalidAlterations.Any()) {
            return Results.BadRequest($"Only these kind of alterations can be applied: " + string.Join(", ", validAlterations.Select(va => va.Name)));
        }

        var sqlStatements = SqliteDatabaseScripter.ScriptAlterations(databaseAlterations);

        connection.Open();
        try {
            foreach (var sql in sqlStatements) {
                connection.Execute(sql);
            }
        }
        catch (Exception ex) {
            return Results.BadRequest(ex);
        }

        return Results.Ok();
    }
}
