using DatabaseMod.Models;

namespace DatabaseMod.Alterations.Models
{
    public class CreateIndex : DatabaseAlteration
    {
        public CreateIndex(string schemaName, string tableName, TableIndex index)
            : base(nameof(CreateIndex))
        {
            SchemaName = schemaName;
            TableName = tableName;
            Index = index;
        }

        public string SchemaName { get; }
        public string TableName { get; }
        public TableIndex Index { get; }
    }
}