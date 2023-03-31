using System.Linq.Expressions;

namespace DataCore;

public readonly struct FilterQuery<TDb, T1> : IQuery<TDb, T1> {
    public FilterQuery(IQuery<TDb, T1> query, Expression<Func<T1, bool>> condition) {
        Query = query;
        Condition = condition;
    }

    public IQuery<TDb, T1> Query { get; }
    public Expression<Func<T1, bool>> Condition { get; }
}