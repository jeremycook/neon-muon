namespace Sqlil;

public static class Quote {
    public static string Identifier(string part) {
        return '"' + part.Replace("\"", "\"\"") + '"';
    }

    public static string Identifier(string separator, params string[] parts) {
        return string.Join(separator, values: parts.SkipWhile(p => p == string.Empty).Select(Identifier));
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
