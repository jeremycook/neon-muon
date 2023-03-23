namespace DatabaseMod.Alterations.Models;

public class CreateSchema : DatabaseAlteration
{
    public CreateSchema(string schemaName)
        : base(nameof(CreateSchema))
    {
        SchemaName = schemaName;
    }

    public CreateSchema(string schemaName, string? owner) : this(schemaName)
    {
        Owner = owner;
    }

    public string SchemaName { get; }
    public string? Owner { get; }
}