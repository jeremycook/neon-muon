using System.ComponentModel.DataAnnotations;
using System.Data;

namespace DatabaseMod.Models;

public class Column
{
    public Column(string name, StoreType storeType, bool isNullable, string? defaultValueSql, string? computedColumnSql)
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

    public StoreType StoreType { get; set; }

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
