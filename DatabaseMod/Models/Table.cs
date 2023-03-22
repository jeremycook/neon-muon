using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public class Table
{
    public Table(string name)
    {
        Name = name;
    }

    [Required, StringLength(50)]
    public string Name { get; set; }

    [StringLength(50)]
    public string? Owner { get; set; }

    public List<Column> Columns { get; set; } = new();

    public List<TableIndex> Indexes { get; set; } = new();
}
