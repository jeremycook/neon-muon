using DatabaseMod.Models;

namespace DatabaseMod.Alterations.Models;

public class CreateTable : DatabaseAlteration {
    public CreateTable(string schemaName, string tableName, Column[] columns, TableIndex[] indexes, TableForeignKey[] foreignKeys, string? owner = null)
        : base(nameof(CreateTable)) {
        SchemaName = schemaName;
        TableName = tableName;
        Columns = columns;
        Indexes = indexes;
        ForeignKeys = foreignKeys;
        Owner = owner;
    }

    public string SchemaName { get; }
    public string TableName { get; }
    public Column[] Columns { get; }
    public TableIndex[] Indexes { get; set; }
    public TableForeignKey[] ForeignKeys { get; }
    public string? Owner { get; }
}