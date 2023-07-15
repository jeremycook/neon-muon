using FileMod;
using Microsoft.Data.Sqlite;
using SqliteMod;
using SqlMod;

namespace WebApiApp;

public class FileEndpoints {
    public record CreateFileInput(string Path);
    public static IResult CreateFile(
        UserFileProvider userFileProvider,
        CreateFileInput input
    ) {
        if (input.Path.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) {
            var fullPath = userFileProvider.GetFullPath(input.Path);

            if (Path.Exists(fullPath)) {
                return Results.BadRequest($"A file already exists at {input.Path}.");
            }

            {
                var builder = new SqliteConnectionStringBuilder() {
                    DataSource = fullPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                };
                using var connection = new SqliteConnection(builder.ConnectionString);
                connection.Open();
                connection.Execute(Sql.Raw("CREATE TABLE Temp (Id INTEGER PRIMARY KEY);"));
                connection.Execute(Sql.Raw("DROP TABLE Temp;"));
            }
            return Results.Ok();
        }
        else {
            userFileProvider.CreateTextFile(input.Path);
            return Results.Ok();
        }
    }

    public record CreateFolderInput(string Path);
    public static void CreateFolder(
        UserFileProvider userFileProvider,
        CreateFolderInput input
    ) {
        userFileProvider.CreateFolder(input.Path);
    }

    public static FileNode GetFileNode(UserFileProvider fileProvider, string path) {
        FileNode fileNode = fileProvider.GetFileNode(path);
        fileNode = WalkFileNode(fileNode, fileNode => {
            if (fileNode.Path.EndsWith(".db")) {
                return DatabaseEndpoints.GetDatabaseFileNode(fileProvider, fileNode);
            }
            else {
                return fileNode;
            }
        });
        return fileNode;
    }

    private static FileNode WalkFileNode(FileNode fileNode, Func<FileNode, FileNode> func) {
        fileNode = func(fileNode);
        if (fileNode.Children?.Any() == true) {
            for (int i = 0; i < fileNode.Children.Count; i++) {
                fileNode.Children[i] = WalkFileNode(fileNode.Children[i], func);
            }
        }
        return fileNode;
    }

    public record DeleteFileInput(string Path);
    public static void DeleteFile(
        UserFileProvider userFileProvider,
        DeleteFileInput input
    ) {
        userFileProvider.Delete(input.Path);
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

    /// <summary>
    /// Upload files into the folder identified by <paramref name="path"/>.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="userFileProvider"></param>
    /// <param name="path">The path to the folder the files should be uploaded into. Missing folders will be created.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IResult UploadFiles(
        HttpContext httpContext,
        UserFileProvider userFileProvider,
        string path
    ) {
        var fullPath = userFileProvider.GetFullPath(path);

        if (!Directory.Exists(fullPath)) {
            Directory.CreateDirectory(fullPath);
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
}
