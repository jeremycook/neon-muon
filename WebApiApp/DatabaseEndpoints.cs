using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using NotebookMod;
using SqliteMod;

namespace WebApiApp;

public class DatabaseEndpoints {

    public static Database GetDatabase(
        NotebookManagerProvider notebookManagerProvider,
        string path
    ) {
        var notebookManager = notebookManagerProvider.GetNotebookManager(path);

        var database = new Database();
        {
            using var connection = notebookManager.CreateConnection();
            connection.Open();
            database.ContributeSqlite(connection);
        }
        return database;
    }

    public static IResult AlterDatabase(
        NotebookManagerProvider notebookManagerProvider,
        string path,
        DatabaseAlteration[] databaseAlterations
    ) {
        var notebookManager = notebookManagerProvider.GetNotebookManager(path);

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

        using var connection = notebookManager.CreateConnection();
        connection.Open();
        try {
            foreach (var sql in sqlStatements) {
                connection.Execute(sql);
            }
        }
        catch (Exception ex) {
            return Results.BadRequest(ex.Message);
        }

        return Results.Ok();
    }
}
