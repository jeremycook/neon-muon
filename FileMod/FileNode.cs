namespace FileMod;

public record FileNode(string Name, string Path, bool IsExpandable, FileNode[]? Children);
