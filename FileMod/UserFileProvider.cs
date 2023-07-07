namespace FileMod;

public class UserFileProvider {
    private readonly string basePath;

    public UserFileProvider(string basePath) {
        if (!Path.IsPathRooted(basePath)) {
            throw new ArgumentException("The basePath must be a rooted/absolute path.", nameof(basePath));
        }

        this.basePath = basePath;
    }

    public bool Exists(string path) {
        return Path.Exists(GetFullPath(path));
    }

    public void Move(string path, string newPath) {
        var fullPath = GetFullPath(path);
        var newFullPath = GetFullPath(newPath);

        if (File.Exists(fullPath)) {
            if (Path.Exists(newFullPath)) {
                throw new ArgumentException($"A file or directory already exists at {newPath}.", nameof(newPath));
            }

            File.Move(fullPath, newFullPath, overwrite: false);
        }
        else if (Directory.Exists(fullPath)) {
            if (Path.Exists(newFullPath)) {
                throw new ArgumentException($"A file or directory already exists at {newPath}.", nameof(newPath));
            }
            Directory.Move(fullPath, newFullPath);
        }
        else {
            throw new ArgumentException($"A file or directory does not exist at {path}.", nameof(path));
        }
    }

    public string GetFullPath(string path) {
        string fullPath = Path.GetFullPath(path, basePath);

        if (!fullPath.StartsWith(basePath)) {
            throw new ArgumentException("The path is invalid.", nameof(path));
        }

        return fullPath;
    }

    public IEnumerable<string> GetChildFileNames(string path) {
        var fullPath = GetFullPath(path);

        var childPaths = Directory.EnumerateFileSystemEntries(fullPath)
            .Select(Path.GetFileName);

        return childPaths!;
    }

    public FileNode GetFileNode(string path) {
        string fullPath = GetFullPath(path);
        string filename = Path.GetFileName(fullPath);

        if (Directory.Exists(fullPath)) {
            return new FileNode(filename, path, true, GetChildFileNames(path).Select(childFileName => GetFileNode(path != string.Empty ? path + "/" + childFileName : childFileName)).ToList());
        }
        else if (File.Exists(fullPath)) {
            return new FileNode(filename, path, false, null);
        }
        else {
            throw new ArgumentException("File or directory not found at " + path, nameof(path));
        }
    }
}
