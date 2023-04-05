namespace DataCore.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public class PrimaryKeyAttribute : Attribute {
    public PrimaryKeyAttribute(params string[] columnNames) {
        ColumnNames = columnNames;
    }

    public IReadOnlyList<string> ColumnNames { get; }
}
