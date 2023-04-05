using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public interface IReadOnlyTableIndex {
    IReadOnlyList<string> Columns { get; }
    TableIndexType IndexType { get; }
    string? Name { get; }

    string GetName(string tableName);
    string GetName(IReadOnlyTable table);
    bool Same(IReadOnlyTableIndex index);
}

public class TableIndex : IReadOnlyTableIndex {
    public TableIndex(string? name, TableIndexType indexType, IEnumerable<string> columns) {
        Name = name;
        IndexType = indexType;
        Columns.AddRange(columns);
    }

    [StringLength(50)]
    public string? Name { get; }

    public TableIndexType IndexType { get; }

    [Required, MinLength(1)]
    public List<string> Columns { get; } = new();

    IReadOnlyList<string> IReadOnlyTableIndex.Columns => Columns;

    public string GetName(IReadOnlyTable table) {
        return GetName(table.Name);
    }

    public string GetName(string tableName) {
        if (!string.IsNullOrEmpty(Name)) {
            return Name;
        }

        return IndexType switch {
            TableIndexType.Index => string.Join("_", new[] { "ix", tableName }.Concat(Columns)),
            TableIndexType.UniqueConstraint => string.Join("_", new[] { "uc", tableName }.Concat(Columns)),
            TableIndexType.PrimaryKey => "pk_" + tableName,
            _ => throw new NotImplementedException(IndexType.ToString()),
        };
    }

    public bool Same(IReadOnlyTableIndex index) {
        return
            Name == index.Name &&
            IndexType == index.IndexType &&
            Columns.SequenceEqual(index.Columns);
    }
}
