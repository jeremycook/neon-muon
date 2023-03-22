namespace DatabaseMod.Alterations.Models
{
    public class ChangeTableOwner : DatabaseAlteration
    {
        public ChangeTableOwner(string schemaName, string tableName, string newOwner)
            : base(nameof(ChangeTableOwner))
        {
            SchemaName = schemaName;
            TableName = tableName;
            NewOwner = newOwner;
        }

        public string SchemaName { get; }
        public string TableName { get; }
        public string NewOwner { get; }
    }
}