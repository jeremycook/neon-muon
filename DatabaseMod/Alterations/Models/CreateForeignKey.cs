using DatabaseMod.Models;

namespace DatabaseMod.Alterations.Models;

public class CreateForeignKey : DatabaseAlteration {
    public CreateForeignKey(string schemaName, string tableName, TableForeignKey foreignKey)
        : base(nameof(CreateForeignKey)) {
        SchemaName = schemaName;
        TableName = tableName;
        ForeignKey = foreignKey;
    }

    public string SchemaName { get; }
    public string TableName { get; }
    public TableForeignKey ForeignKey { get; }
}