namespace Sqlil;

public interface ISqlil { }

public readonly record struct SqlTable(Type Table) : ISqlil {
    public static SqlTable Create(Type Table) => new(Table);

    public override string ToString() {
        return Quote.Identifier(Table.Name);
    }
}

public readonly record struct SqlAlias(ISqlil Inner, string Alias) : ISqlil {
    public static SqlAlias Create(ISqlil Inner, string Alias) => new(Inner, Alias);

    public override string ToString() {
        return Inner switch {
            SqlIdentifier => $"{Inner} {Quote.Identifier(Alias)}",
            SqlTable => $"{Inner} {Quote.Identifier(Alias)}",
            _ => $"({Inner}) {Quote.Identifier(Alias)}",
        };
    }
}

public readonly record struct SqlKeyword(string Keyword) : ISqlil {
    public static SqlKeyword Create(string Keyword) => new(Keyword);

    public override string ToString() {
        return Keyword;
    }
}

public readonly record struct SqlIdentifier(string Prefix, string Identifier) : ISqlil {
    public static SqlIdentifier Create(string Prefix, string Identifier) => new(Prefix, Identifier);
    public static SqlIdentifier Create(string Identifier) => new(string.Empty, Identifier);

    public override string ToString() {
        return Quote.Identifier(".", Prefix, Identifier);
    }
}

public readonly record struct SqlParameter(Type Type) : ISqlil {
    public static SqlParameter Create(Type Type) => new(Type);

    public override string ToString() {
        return "<" + Type.Name + ">";
    }
}

public readonly record struct SqlSelect(IReadOnlyList<ISqlil> Selectors) : ISqlil {
    public static SqlSelect Create(IReadOnlyList<ISqlil> Selectors) => new(Selectors);

    public override string ToString() {
        return "SELECT " + string.Join(", ", Selectors);
    }
}

//public IEnumerable<(string, string)> ResultColumn(Type type)
//{
//	var under = Nullable.GetUnderlyingType(type) ?? type;
//	if (under.Name.StartsWith("ValueTuple`"))
//	{
//		return under.GetFields().SelectMany(f => IsPrimitive(f.FieldType)
//			? new[] { (f.Name, f.Name) }
//			: ResultColumn(f.FieldType).Select((tuple) => (f.Name + "." + tuple.Item1, tuple.Item2)));
//	}
//	else if (IsPrimitive(under))
//	{
//		throw new NotSupportedException();
//	}
//	else
//	{
//		return under.GetProperties().Select(prop => (prop.Name, prop.PropertyType.Name + "." + prop.Name));
//	}
//}
