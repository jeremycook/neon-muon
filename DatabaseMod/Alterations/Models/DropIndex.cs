using DatabaseMod.Models;

namespace DatabaseMod.Alterations.Models;

public class DropIndex : DatabaseAlteration
{
    public DropIndex(string schemaName, string tableName, IReadOnlyTableIndex index)
        : base(nameof(DropIndex))
    {
        SchemaName = schemaName;
        TableName = tableName;
        Index = index;
    }

    public string SchemaName { get; }
    public string TableName { get; }
    public IReadOnlyTableIndex Index { get; }


    public override string ToString()
    {
        return $"EX: DROP INDEX {SchemaName}.{TableName}.{Index.Name}";
    }
}