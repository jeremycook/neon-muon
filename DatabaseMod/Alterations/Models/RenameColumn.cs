namespace DatabaseMod.Alterations.Models
{
    public class RenameColumn : DatabaseAlteration
    {
        public RenameColumn(string schemaName, string tableName, string columnName, string newColumnName)
            : base(nameof(RenameColumn))
        {
            SchemaName = schemaName;
            TableName = tableName;
            ColumnName = columnName;
            NewColumnName = newColumnName;
        }

        public string SchemaName { get; }
        public string TableName { get; }
        public string ColumnName { get; }
        public string NewColumnName { get; }
    }
}