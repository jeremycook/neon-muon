namespace DataCore.EF;

public interface IComponentDbContext
{
}

public interface IComponentDbContext<TDb> : IComponentDbContext
{
    IQueryable<T> Queryable<T>()
        where T : class;
}
