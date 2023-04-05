using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public interface IReadOnlyColumn {
    string? ComputedColumnSql { get; }
    string? DefaultValueSql { get; }
    bool IsNullable { get; }
    string Name { get; }
    int Position { get; }
    StoreType StoreType { get; }

    bool Same(IReadOnlyColumn column);
}

public class Column : IReadOnlyColumn {
    public Column(string name, StoreType storeType, bool isNullable, string? defaultValueSql, string? computedColumnSql) {
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

    public bool Same(IReadOnlyColumn column) {
        return
            Position == column.Position &&
            Name == column.Name &&
            StoreType == column.StoreType &&
            IsNullable == column.IsNullable &&
            DefaultValueSql == column.DefaultValueSql &&
            ComputedColumnSql == column.ComputedColumnSql;
    }
}
