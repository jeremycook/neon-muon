using FileMod;
using Microsoft.Extensions.FileProviders;

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
        else {
            return new FileNode(fileInfo.Name, subpath, false, null);
        }
    }

    private static FileNode GetFileNode(IFileProvider fileProvider, IFileInfo fileInfo, string prefix) {
        string subpath = prefix + "/" + fileInfo.Name;
        if (fileInfo.IsDirectory) {
            return new FileNode(fileInfo.Name, subpath, true, fileProvider.GetDirectoryContents(subpath).Select(fi => GetFileNode(fileProvider, fi, subpath)).ToArray());
        }
        else {
            return new FileNode(fileInfo.Name, subpath, false, null);
        }
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
