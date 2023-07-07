using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using FileMod;
using Microsoft.Data.Sqlite;
using SqliteMod;

namespace WebApiApp;

public class DatabaseEndpoints {

    public static Database GetDatabase(
        UserFileProvider fileProvider,
        string path
    ) {
        var fullPath = fileProvider.GetFullPath(path);

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadOnly,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        var database = connection.GetDatabase();
        return database;
    }

    public static IResult AlterDatabase(
        UserFileProvider fileProvider,
        string path,
        DatabaseAlteration[] databaseAlterations
    ) {
        var fullPath = fileProvider.GetFullPath(path);

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

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fullPath,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using (var transaction = connection.BeginTransaction()) {
            try {
                foreach (var sql in sqlStatements) {
                    connection.Execute(sql);
                }
            }
            catch (Exception ex) {
                return Results.BadRequest(ex.Message);
            }

            transaction.Commit();
        }

        return Results.Ok();
    }
}
