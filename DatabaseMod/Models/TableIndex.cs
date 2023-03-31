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
    public TableIndex(string? name, TableIndexType indexType) {
        Name = name;
        IndexType = indexType;
    }

    [StringLength(50)]
    public string? Name { get; set; }

    public TableIndexType IndexType { get; set; }

    [Required, MinLength(1)]
    public List<string> Columns { get; set; } = new();

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
