using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public class Column
{
    public Column(string name, string storeType, bool isNullable, string? defaultValueSql, string? computedColumnSql)
    {
        Name = name;
        StoreType = storeType;
        IsNullable = isNullable;
        DefaultValueSql = defaultValueSql;
        ComputedColumnSql = computedColumnSql;
    }

    public int Position { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; }

    [Required, StringLength(50), RegularExpression("^[a-zA-Z]+$", ErrorMessage = "The {0} may only contain letters.")]
    public string StoreType { get; set; }

    public bool IsNullable { get; set; }

    public string? DefaultValueSql { get; set; }

    public string? ComputedColumnSql { get; set; }

    public bool Same(Column column)
    {
        return
            Position == column.Position &&
            Name == column.Name &&
            StoreType == column.StoreType &&
            IsNullable == column.IsNullable &&
            DefaultValueSql == column.DefaultValueSql &&
            ComputedColumnSql == column.ComputedColumnSql;
    }
}
