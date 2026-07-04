using System.Linq.Expressions;

namespace Sqlil.Core;

public static class QueryableExtensions {
    public static IQueryable<T> Insert<T>(this IQueryable<T> source, T value) where T : struct {
        return source;
    }

    public static IQueryable<T> Update<T>(this IQueryable<T> source, Expression<Func<T, T>> map) where T : struct {
        return source;
    }

    public static IQueryable<T> Delete<T>(this IQueryable<T> source) where T : struct {
        return source;
    }
}
