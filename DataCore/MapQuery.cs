using System.Linq.Expressions;

namespace DataCore;

public static class MapQuery {
    public static MapQuery<TDb, T, T> CreateIdentityMap<TDb, T>(IQuery<TDb, T> query) {
        return new MapQuery<TDb, T, T>(query, o => o);
    }
}

public readonly struct MapQuery<TDb, TSource, T> : IQuery<TDb, T> {
    public MapQuery(IQuery<TDb, TSource> query, Expression<Func<TSource, T>> map) {
        Query = query;
        Map = map;
    }

    public QueryType QueryType => QueryType.Map;
    public IQuery<TDb, TSource> Query { get; }
    public Expression<Func<TSource, T>> Map { get; }
}