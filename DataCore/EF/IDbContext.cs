namespace DataCore.EF;

public interface IDbContext
{
}

public interface IDbContext<TDb> : IDbContext
{
    IQueryable<T> Queryable<T>()
        where T : class;
}
