using DatabaseMod.Models;
using FileMod;
using Microsoft.Data.Sqlite;
using SqliteMod;

namespace WebApiApp;

public class FileEndpoints {
    public record CreateFileInput(string Path);
    public static void CreateFile(
        UserFileProvider userFileProvider,
        CreateFileInput input
    ) {
        userFileProvider.CreateTextFile(input.Path);
    }

    public static FileNode GetFileNode(UserFileProvider fileProvider, string path) {

        FileNode fileNode;
        if (path.EndsWith(".db")) {
            fileNode = GetDatabaseFileNode(fileProvider, path);
        }
        else {
            fileNode = fileProvider.GetFileNode(path);
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

    public static IResult UploadFiles(
        HttpContext httpContext,
        UserFileProvider userFileProvider,
        string path
    ) {
        var fullPath = userFileProvider.GetFullPath(path);

        if (!Directory.Exists(fullPath)) {
            throw new ArgumentException($"The path is invalid.", nameof(path));
        }

        var prefix = path != string.Empty ? path + '/' : string.Empty;
        var uploads = httpContext.Request.Form.Files
            .Select(file => (
                file,
                path: prefix + file.FileName,
                destinationPath: userFileProvider.GetFullPath(prefix + file.FileName)
            ))
            .ToArray();

        // TODO: Rename files (i.e. "filename 1.jpg", "filename 2.jpg", etc.) if they already exist instead of erroring out
        var alreadyExists = uploads.Where(upload => Path.Exists(upload.destinationPath));
        if (alreadyExists.Any()) {
            return Results.BadRequest($"The following files already exist: {string.Join("; ", alreadyExists.Select(x => x.path))}. No files were uploaded.");
        }

        foreach (var (file, _, destinationPath) in uploads) {
            using var source = file.OpenReadStream();
            using var destination = File.Open(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(destination);
        }

        return Results.Ok();
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
