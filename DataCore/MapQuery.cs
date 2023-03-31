using System.Linq.Expressions;

namespace DataCore;

public readonly struct MapQuery<TDb, T1, TMapped> : IQuery<TDb, TMapped> {
    public MapQuery(IQuery<TDb, T1> query, Expression<Func<T1, TMapped>> map) {
        Query = query;
        Map = map;
    }

    public QueryType Type => QueryType.Map;
    public IQuery<TDb, T1> Query { get; }
    public Expression<Func<T1, TMapped>> Map { get; }
}