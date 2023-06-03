using System.Text.Json;
using System.Text.Json.Serialization;

namespace DatabaseMod.Alterations.Models;

[JsonDerivedType(typeof(AlterColumn), nameof(AlterColumn))]
[JsonDerivedType(typeof(AlterIndex), nameof(AlterIndex))]
[JsonDerivedType(typeof(ChangeTableOwner), nameof(ChangeTableOwner))]
[JsonDerivedType(typeof(CreateColumn), nameof(CreateColumn))]
[JsonDerivedType(typeof(CreateIndex), nameof(CreateIndex))]
[JsonDerivedType(typeof(CreateSchema), nameof(CreateSchema))]
[JsonDerivedType(typeof(CreateTable), nameof(CreateTable))]
[JsonDerivedType(typeof(DropColumn), nameof(DropColumn))]
[JsonDerivedType(typeof(DropIndex), nameof(DropIndex))]
[JsonDerivedType(typeof(DropTable), nameof(DropTable))]
[JsonDerivedType(typeof(RenameColumn), nameof(RenameColumn))]
[JsonDerivedType(typeof(RenameTable), nameof(RenameTable))]
public abstract class DatabaseAlteration
{
    protected DatabaseAlteration(string type)
    {
        Type = type;
    }

    [JsonPropertyName("$type"), JsonPropertyOrder(-1)]
    public string Type { get; }

    public override string ToString()
    {
        return $"{base.ToString()}: {JsonSerializer.Serialize(this)}";
    }
}