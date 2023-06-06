using Microsoft.Extensions.FileProviders;

namespace NotebookMod;

public class NotebookManagerProvider {
    public NotebookManager GetNotebookManager(string notebookSubpath) {
        var manager = new NotebookManager(userFileProvider, notebookSubpath);
        return manager;
    }

    private readonly PhysicalFileProvider userFileProvider;

    public NotebookManagerProvider(PhysicalFileProvider userFileProvider) {
        this.userFileProvider = userFileProvider;
    }
}
