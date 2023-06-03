namespace DatabaseMod.Alterations.Models;

public class DropTable : DatabaseAlteration {
    public DropTable(string schemaName, string tableName)
        : base(nameof(DropTable)) {
        SchemaName = schemaName;
        TableName = tableName;
    }

    public string SchemaName { get; }
    public string TableName { get; }
}
