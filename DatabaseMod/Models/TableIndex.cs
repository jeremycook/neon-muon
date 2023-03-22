using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public class TableIndex
{
    public TableIndex(string? name, TableIndexType indexType)
    {
        Name = name;
        IndexType = indexType;
    }

    [StringLength(50)]
    public string? Name { get; set; }

    public TableIndexType IndexType { get; set; }

    [Required, MinLength(1)]
    public List<string> Columns { get; set; } = new();

    public string GetName(Table table)
    {
        return GetName(table.Name);
    }

    public string GetName(string tableName)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return Name;
        }

        return IndexType switch
        {
            TableIndexType.Index => string.Join("_", new[] { "ix", tableName }.Concat(Columns)),
            TableIndexType.UniqueConstraint => string.Join("_", new[] { "uc", tableName }.Concat(Columns)),
            TableIndexType.PrimaryKey => "pk_" + tableName,
            _ => throw new NotImplementedException(IndexType.ToString()),
        };
    }

    public bool Same(TableIndex index)
    {
        return
            Name == index.Name &&
            IndexType == index.IndexType &&
            Columns.SequenceEqual(index.Columns);
    }
}
