namespace FileMod;

public record FileNode(string Name, string Path, bool IsDirectory, FileNode[]? Children);
