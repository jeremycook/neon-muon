using FileMod;
using Microsoft.Extensions.FileProviders;

namespace NotebookMod;

public class NotebookManagerProvider {
    public NotebookManager GetNotebookManager(string notebookSubpath) {
        var manager = new NotebookManager(userFileProvider, notebookSubpath);
        return manager;
    }

    private readonly UserFileProvider userFileProvider;

    public NotebookManagerProvider(UserFileProvider userFileProvider) {
        this.userFileProvider = userFileProvider;
    }
}
