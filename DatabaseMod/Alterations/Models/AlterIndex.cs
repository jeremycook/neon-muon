using DatabaseMod.Models;

namespace DatabaseMod.Alterations.Models;

public class AlterIndex : DatabaseAlteration
{
    public AlterIndex(string schemaName, string tableName, TableIndex index)
        : base(nameof(AlterIndex))
    {
        SchemaName = schemaName;
        TableName = tableName;
        Index = index;
    }

    public string SchemaName { get; }
    public string TableName { get; }
    public TableIndex Index { get; }
}
