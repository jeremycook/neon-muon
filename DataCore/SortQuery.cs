using System.Linq.Expressions;

namespace DataCore;

public readonly struct SortQuery<TDb, T1, TProperty> : IQuery<TDb, T1> {
    public SortQuery(IQuery<TDb, T1> query, Expression<Func<T1, TProperty>> sort, SortDirection asc) {
        Query = query;
        Sort = sort;
        Asc = asc;
    }

    public QueryType Type => QueryType.Sort;
    public IQuery<TDb, T1> Query { get; }
    public Expression<Func<T1, TProperty>> Sort { get; }
    public SortDirection Asc { get; }
}
