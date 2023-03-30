using System.Linq.Expressions;

namespace DataCore;

public interface IQueryContext<TDb>
    where TDb : IDb<TDb>
{
    IQuery<TDb, T> From<T>(Expression<Func<TDb, IQuery<TDb, T>>> query);
}