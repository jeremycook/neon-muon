using DatabaseMod.Models;

namespace DatabaseMod.Alterations.Models;

public class CreateColumn : DatabaseAlteration
{
    public CreateColumn(string schemaName, string tableName, IReadOnlyColumn column)
        : base(nameof(CreateColumn))
    {
        SchemaName = schemaName;
        TableName = tableName;
        Column = column;
    }

    public string SchemaName { get; }
    public string TableName { get; }
    public IReadOnlyColumn Column { get; }
}