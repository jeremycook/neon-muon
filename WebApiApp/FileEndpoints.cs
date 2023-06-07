using FileMod;

namespace WebApiApp;

public static class FileEndpoints {
    public static IResult GetFile(
        UserFileProvider userFileProvider,
        string path
    ) {
        var fileInfo = userFileProvider.GetFileInfo(path);

        if (!fileInfo.Exists) {
            return Results.NotFound();
        }

        return Results.File(fileInfo.PhysicalPath!);
    }
}
