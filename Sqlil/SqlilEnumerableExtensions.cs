namespace Sqlil;

public static class SqlilEnumerableExtensions {
    // Lifts the enumerable into a remote context that can be queried
    public static IQueryable<T> Lift<T>(this IEnumerable<T> enumerable) {
        return enumerable.AsQueryable();
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
