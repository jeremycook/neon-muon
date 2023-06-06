using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileProviders;

namespace NotebookMod;

public class NotebookManager {

    public SqliteConnection CreateConnection() {
        var fileInfo = fileProvider.GetFileInfo(notebookSubpath);
        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fileInfo.PhysicalPath!,
        };
        return new SqliteConnection(builder.ConnectionString);
    }

    private readonly PhysicalFileProvider fileProvider;
    private readonly string notebookSubpath;

    public NotebookManager(PhysicalFileProvider notebookFileProvider, string notebookSubpath) {
        this.fileProvider = notebookFileProvider;
        this.notebookSubpath = notebookSubpath;
    }
}