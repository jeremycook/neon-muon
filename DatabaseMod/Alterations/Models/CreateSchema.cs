namespace DatabaseMod.Alterations.Models
{
    public class CreateSchema : DatabaseAlteration
    {
        public CreateSchema(string schemaName)
            : base(nameof(CreateSchema))
        {
            SchemaName = schemaName;
        }

        public string SchemaName { get; }
    }
}