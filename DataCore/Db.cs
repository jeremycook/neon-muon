using DataCore.EF;
using System.Linq.Expressions;

namespace DataCore;

public class Db<TDb> : IDb<TDb>
    where TDb : IDb<TDb>
{
    public Db(IComponentDbContext<TDb> dbContext)
    {
        DbContext = dbContext;
    }

    public IQuery<TDb, T> From<T>()
    {
        throw new NotImplementedException();
    }

    public IQuery<TDb, T> From<T>(Expression<Func<TDb, IQuery<TDb, T>>> query)
    {
        throw new NotImplementedException();
    }

    protected IComponentDbContext<TDb> DbContext { get; }
}
