namespace DataCore.EF;

public interface IDbContext {
    IQueryable<T> Queryable<T>()
        where T : class;
}

public interface IDbContext<TDb> : IDbContext { }
