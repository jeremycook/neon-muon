namespace FileMod;

// TODO: Change IsExpandable to HasChildren
public record FileNode(string Name, string Path, bool IsExpandable, List<FileNode>? Children);
