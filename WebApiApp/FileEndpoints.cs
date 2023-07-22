using FileMod;
using Microsoft.Data.Sqlite;
using SqliteMod;
using SqlMod;

namespace WebApiApp;

public class FileEndpoints {
    public record CreateFileInput(string Path);
    public static IResult CreateFile(
        UserData userData,
        CreateFileInput input
    ) {
        if (input.Path.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) {
            if (userData.Exists(input.Path)) {
                return Results.BadRequest($"A file already exists at {input.Path}.");
            }

            {
                using var connection = new SqliteConnection(userData.GetConnectionString(input.Path, SqliteOpenMode.ReadWriteCreate));
                connection.Open();
                connection.Execute(Sql.Raw("CREATE TABLE Temp (Id INTEGER PRIMARY KEY);"));
                connection.Execute(Sql.Raw("DROP TABLE Temp;"));
            }
            return Results.Ok();
        }
        else {
            userData.CreateTextFile(input.Path);
            return Results.Ok();
        }
    }

    public record CreateFolderInput(string Path);
    public static void CreateFolder(
        UserData userData,
        CreateFolderInput input
    ) {
        userData.CreateFolder(input.Path);
    }

    public static FileNode GetFileNode(UserData userData, string path) {
        FileNode fileNode = userData.GetFileNode(path);
        fileNode = WalkFileNode(fileNode, fileNode => {
            if (fileNode.Path.EndsWith(".db")) {
                return DatabaseEndpoints.GetDatabaseFileNode(userData, fileNode);
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
        UserData userData,
        DeleteFileInput input
    ) {
        userData.Delete(input.Path);
    }

    public static IResult DownloadFile(
        UserData userData,
        string path
    ) {
        var physicalPath = userData.GetFullPath(path);
        return Results.File(physicalPath, fileDownloadName: Path.GetFileName(path));
    }

    public record MoveFileInput(string Path, string NewPath);
    public static void MoveFile(
        UserData userData,
        MoveFileInput input
    ) {
        userData.Move(input.Path, input.NewPath);
    }

    /// <summary>
    /// Create or replace content via a multipart/form-data request. The filename of each file's Content-Disposition is interpreted as the relative path to upload that file to.
    /// </summary>
    /// <param name="uploads"></param>
    /// <param name="userData"></param>
    /// <returns></returns>
    public static IResult UploadContent(
        IFormFileCollection uploads,
        UserData userData
    ) {
        foreach (var file in uploads) {
            var fullPath = userData.GetFullPath(file.FileName);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var source = file.OpenReadStream();
            using var destination = File.Open(fullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            source.CopyTo(destination);
        }

        return Results.Ok();
    }

    /// <summary>
    /// Upload files into the folder identified by <paramref name="path"/>.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="userData"></param>
    /// <param name="path">The path to the folder the files should be uploaded into. Missing folders will be created.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IResult UploadFiles(
        HttpContext httpContext,
        UserData userData,
        string path
    ) {
        var fullPath = userData.GetFullPath(path);

        if (!Directory.Exists(fullPath)) {
            Directory.CreateDirectory(fullPath);
        }

        var prefix = path != string.Empty ? path + '/' : string.Empty;
        var uploads = httpContext.Request.Form.Files
            .Select(file => (
                file,
                path: prefix + file.FileName,
                destinationPath: userData.GetFullPath(prefix + file.FileName)
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
