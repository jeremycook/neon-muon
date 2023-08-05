using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public interface IReadOnlyTable {
    IReadOnlyList<IReadOnlyColumn> Columns { get; }
    IReadOnlyList<IReadOnlyTableIndex> Indexes { get; }
    IReadOnlyList<TableForeignKey> ForeignKeys { get; }
    string Name { get; }
    string? Owner { get; }
    TableIndex? GetPrimaryKey();
}

public class Table : IReadOnlyTable {
    public Table(string name) {
        Name = name;
    }

    [Required, StringLength(50)]
    public string Name { get; set; }

    [StringLength(50)]
    public string? Owner { get; set; }

    public List<Column> Columns { get; set; } = new();

    public List<TableIndex> Indexes { get; set; } = new();
    
    public List<TableForeignKey> ForeignKeys { get; set; } = new();

    IReadOnlyList<IReadOnlyColumn> IReadOnlyTable.Columns => Columns;
    IReadOnlyList<IReadOnlyTableIndex> IReadOnlyTable.Indexes => Indexes;
    IReadOnlyList<TableForeignKey> IReadOnlyTable.ForeignKeys => ForeignKeys;

    public TableIndex GetPrimaryKey() {
        return Indexes.Single(x => x.IndexType == TableIndexType.PrimaryKey);
    }
}
