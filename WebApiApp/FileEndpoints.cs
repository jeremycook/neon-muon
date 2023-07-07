using DatabaseMod.Models;
using FileMod;
using Microsoft.Data.Sqlite;
using SqliteMod;

namespace WebApiApp;

public class FileEndpoints {
    public static FileNode GetFileNode(UserFileProvider fileProvider, string path) {
        FileNode fileNode = fileProvider.GetFileNode(path);

        if (fileNode.Name.EndsWith(".db")) {
            fileNode = GetDatabaseFileNode(fileProvider, path);
        }

        return fileNode;
    }

    public static IResult DownloadFile(
        UserFileProvider userFileProvider,
        string path
    ) {
        var physicalPath = userFileProvider.GetFullPath(path);
        return Results.File(physicalPath, fileDownloadName: Path.GetFileName(path));
    }

    public record MoveFileInput(string Path, string NewPath);
    public static void MoveFile(
        UserFileProvider userFileProvider,
        MoveFileInput input
    ) {
        userFileProvider.Move(input.Path, input.NewPath);
    }

    private static FileNode GetDatabaseFileNode(UserFileProvider fileProvider, string path) {
        string fullPath = fileProvider.GetFullPath(path);
        string filename = Path.GetFileName(fullPath);

        var database = new Database();
        {
            var builder = new SqliteConnectionStringBuilder() {
                DataSource = fullPath,
                Mode = SqliteOpenMode.ReadOnly,
            };
            using var connection = new SqliteConnection(builder.ConnectionString);
            connection.Open();
            database.ContributeSqlite(connection);
        }

        var children = database.Schemas
            .SelectMany(schema => schema.Name == string.Empty
                ? schema.Tables.Select(table => new FileNode(table.Name, path + "/main/" + table.Name, false, null))
                : schema.Tables.Select(table => new FileNode(table.Name, path + "/" + schema.Name + "/" + table.Name, false, null))
            )
            .OrderBy(table => table.Name)
            .ToList();

        return new FileNode(filename, path, true, children);
    }
}
