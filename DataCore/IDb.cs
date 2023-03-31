using System.Linq.Expressions;

namespace DataCore;

public interface IDb
{
}

public interface IDb<TDb> : IDb
    where TDb : IDb<TDb>, IDb
{
    IQuery<TDb, T> From<T>();
}
