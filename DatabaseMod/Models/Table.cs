using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public interface IReadOnlyTable {
    IReadOnlyList<IReadOnlyColumn> Columns { get; }
    IReadOnlyList<IReadOnlyTableIndex> Indexes { get; }
    string Name { get; }
    string? Owner { get; }
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

    IReadOnlyList<IReadOnlyColumn> IReadOnlyTable.Columns => Columns;
    IReadOnlyList<IReadOnlyTableIndex> IReadOnlyTable.Indexes => Indexes;
}
