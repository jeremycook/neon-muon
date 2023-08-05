using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public class TableForeignKey {
    public TableForeignKey(
        string? name,
        IEnumerable<string> columns,
        string foreignTableSchema,
        string foreignTableName,
        IEnumerable<string> foreignColumns
    ) {
        Name = name;
        Columns.AddRange(columns);
        ForeignSchemaName = foreignTableSchema;
        ForeignTableName = foreignTableName;
        ForeignColumns.AddRange(foreignColumns);
    }

    [StringLength(50)]
    public string? Name { get; }

    [Required, MinLength(1)]
    public List<string> Columns { get; } = new();

    public string ForeignSchemaName { get; }
    public string ForeignTableName { get; }
    public List<string> ForeignColumns { get; } = new();

    public string GetName(IReadOnlyTable table) {
        return GetName(table.Name);
    }

    public string GetName(string tableName) {
        if (!string.IsNullOrEmpty(Name)) {
            return Name;
        }

        return "fk_" + tableName + "_" + string.Join("_", Columns);
    }

    public bool Equivalent(TableForeignKey other) =>
        Columns.SequenceEqual(other.Columns) &&
        ForeignSchemaName == other.ForeignSchemaName &&
        ForeignTableName == other.ForeignTableName &&
        ForeignColumns.SequenceEqual(other.ForeignColumns);
}
