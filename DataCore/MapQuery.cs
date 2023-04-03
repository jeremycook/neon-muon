using System.Linq.Expressions;

namespace DataCore;

public static class MapQuery {
    public static MapQuery<TDb, T1, T1> CreateIdentityMap<TDb, T1>(IQuery<TDb, T1> query) {
        return new MapQuery<TDb, T1, T1>(query, o => o);
    }
}

public readonly struct MapQuery<TDb, T1, TMapped> : IQuery<TDb, TMapped> {
    public MapQuery(IQuery<TDb, T1> query, Expression<Func<T1, TMapped>> map) {
        Query = query;
        Map = map;
    }

    public QueryType QueryType => QueryType.Map;
    public IQuery<TDb, T1> Query { get; }
    public Expression<Func<T1, TMapped>> Map { get; }
}