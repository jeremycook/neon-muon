namespace DataCore.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public class PrimaryKeyAttribute : Attribute {
    public PrimaryKeyAttribute(params string[] columns) {
        Columns = columns;
    }

    public IReadOnlyList<string> Columns { get; }
}
