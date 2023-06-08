using DatabaseMod.Models;
using FileMod;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileProviders;
using SqliteMod;

namespace WebApiApp;

public static class FileEndpoints {
    public static FileNode GetRootFileNode(
        UserFileProvider fileProvider
    ) {
        var rootFileNode = new FileNode("", "", true, fileProvider.GetDirectoryContents("").Select(fi => GetFileNode(fileProvider, fi)).ToArray());
        return rootFileNode;
    }

    private static FileNode GetFileNode(IFileProvider fileProvider, IFileInfo fileInfo) {
        string subpath = fileInfo.Name;
        if (fileInfo.IsDirectory) {
            return new FileNode(fileInfo.Name, subpath, true, fileProvider.GetDirectoryContents(subpath).Select(fi => GetFileNode(fileProvider, fi, subpath)).ToArray());
        }
        else if (fileInfo.Name.EndsWith(".db")) {
            return GetDatabaseFileNode(fileInfo, subpath);
        }
        else {
            return new FileNode(fileInfo.Name, subpath, false, null);
        }
    }

    private static FileNode GetFileNode(IFileProvider fileProvider, IFileInfo fileInfo, string prefix) {
        string subpath = prefix + "/" + fileInfo.Name;
        if (fileInfo.IsDirectory) {
            return new FileNode(fileInfo.Name, subpath, true, fileProvider.GetDirectoryContents(subpath).Select(fi => GetFileNode(fileProvider, fi, subpath)).ToArray());
        }
        else if (fileInfo.Name.EndsWith(".db")) {
            return GetDatabaseFileNode(fileInfo, subpath);
        }
        else {
            return new FileNode(fileInfo.Name, subpath, false, null);
        }
    }

    /// <summary>
    /// List the database and its tables
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="subpath"></param>
    /// <returns></returns>
    private static FileNode GetDatabaseFileNode(IFileInfo fileInfo, string subpath) {
        var database = new Database();
        {
            var builder = new SqliteConnectionStringBuilder() {
                DataSource = fileInfo.PhysicalPath!,
            };
            using var connection = new SqliteConnection(builder.ConnectionString);
            connection.Open();
            database.ContributeSqlite(connection);
        }

        FileNode[] children = database.Schemas
            .SelectMany(schema => schema.Tables)
            .Select(table => new FileNode(table.Name, subpath + "/" + table.Name, false, null))
            .ToArray();

        return new FileNode(fileInfo.Name, subpath, true, children);
    }

    public static IResult GetFile(
        UserFileProvider userFileProvider,
        string path
    ) {
        var fileInfo = userFileProvider.GetFileInfo(path);

        if (!fileInfo.Exists) {
            return Results.NotFound();
        }

        return Results.File(fileInfo.PhysicalPath!, fileDownloadName: fileInfo.Name);
    }
}
