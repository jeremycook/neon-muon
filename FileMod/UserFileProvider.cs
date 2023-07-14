using System.Text;

namespace FileMod;

public class UserFileProvider {
    private readonly string basePath;

    public UserFileProvider(string basePath) {
        if (!Path.IsPathRooted(basePath)) {
            throw new ArgumentException("The basePath must be a rooted/absolute path.", nameof(basePath));
        }

        this.basePath = basePath;
    }

    /// <summary>
    /// Creates a folder at <paramref name="path"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public void CreateFolder(string path) {
        var fullPath = GetFullPath(path);

        if (Path.Exists(fullPath)) {
            throw new ArgumentException($"A file or directory already exists at {path}.", nameof(path));
        }

        Directory.CreateDirectory(fullPath);
    }

    /// <summary>
    /// Creates a UTF-8 text file at <paramref name="path"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public void CreateTextFile(string path) {
        var fullPath = GetFullPath(path);

        if (Path.Exists(fullPath)) {
            throw new ArgumentException($"A file or directory already exists at {path}.", nameof(path));
        }

        File.WriteAllText(fullPath, "", Encoding.UTF8);
    }

    public void Delete(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException($"The root directory cannot be deleted.", nameof(path));
        }

        var fullPath = GetFullPath(path);

        if (Directory.Exists(fullPath)) {
            Directory.Delete(fullPath, recursive: true);
        }
        else if (File.Exists(fullPath)) {
            File.Delete(fullPath);
        }
    }

    public bool Exists(string path) {
        return Path.Exists(GetFullPath(path));
    }

    /// <summary>
    /// Changes the file or directory at <paramref name="path"/> to <paramref name="newPath"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="newPath"></param>
    /// <exception cref="ArgumentException"></exception>
    public void Move(string path, string newPath) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException($"The root directory cannot be moved.", nameof(path));
        }

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
        string filename = path == string.Empty
            ? "Root"
            : Path.GetFileName(fullPath);

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
