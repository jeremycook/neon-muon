using System.ComponentModel.DataAnnotations;

namespace DatabaseMod.Models;

public interface IReadOnlySchema {
    string Name { get; }
    string? Owner { get; }
    IReadOnlyList<IReadOnlyTable> Tables { get; }
    IReadOnlyList<IReadOnlySchemaPrivileges> Privileges { get; }
    IReadOnlyList<IReadOnlyDefaultPrivileges> DefaultPrivileges { get; }
}

public class Schema : IReadOnlySchema {
    public const string DefaultName = "";

    public Schema(string name = DefaultName) {
        Name = name;
    }

    [Required, StringLength(50)]
    public string Name { get; set; }

    [StringLength(50)]
    public string? Owner { get; set; }

    public List<Table> Tables { get; set; } = new();
    public List<SchemaPrivileges> Privileges { get; set; } = new();
    public List<DefaultPrivileges> DefaultPrivileges { get; set; } = new();

    IReadOnlyList<IReadOnlyDefaultPrivileges> IReadOnlySchema.DefaultPrivileges => DefaultPrivileges;
    IReadOnlyList<IReadOnlySchemaPrivileges> IReadOnlySchema.Privileges => Privileges;
    IReadOnlyList<IReadOnlyTable> IReadOnlySchema.Tables => Tables;
}
