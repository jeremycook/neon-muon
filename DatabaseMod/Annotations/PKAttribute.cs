namespace DatabaseMod.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public class PKAttribute : Attribute {
    public PKAttribute(params string[] columns) {
        Columns = columns;
    }

    public IReadOnlyList<string> Columns { get; }
}
