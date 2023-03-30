using System.Linq.Expressions;

namespace DataCore;

public static class Query
{
    public static IQueryContext<TDb> Context<TDb>(IDb<TDb> db)
        where TDb : IDb<TDb>
    {
        throw new NotImplementedException();
    }

    public static IQuery<TDb, T> From<TDb, T>(IDb<TDb> db, Expression<Func<TDb, T>> selector)
        where TDb : IDb<TDb>
    {
        throw new NotImplementedException();
    }

    public static IQuery<TDb, (T1, T2)> Join<TDb, T1, T2>(
        IDb<TDb> db,
        Expression<Func<TDb, IQuery<TDb, T1>>> selector1,
        Expression<Func<TDb, IQuery<TDb, T2>>> selector2,
        Expression<Func<T1, T2, bool>> predicate)
        where TDb : IDb<TDb>
    {
        throw new NotImplementedException();
    }
}

//public class Query<T> : IQuery<T>
//{
//    public IQuery<T> Asc<TProperty>(Expression<Func<T, TProperty>> sort)
//    {
//        throw new NotImplementedException();
//    }

//    public IQuery<T> Desc<TProperty>(Expression<Func<T, TProperty>> sort)
//    {
//        throw new NotImplementedException();
//    }

//    public IQuery<T> Filter(Expression<Func<T, bool>> predicate)
//    {
//        throw new NotImplementedException();
//    }

//    public IQuery<TProjected> Map<TProjected>(Expression<Func<T, TProjected>> projection)
//    {
//        throw new NotImplementedException();
//    }

//    public ValueTask<List<T>> ToListAsync<TProperty>(CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }
//}
