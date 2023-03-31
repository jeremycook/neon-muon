using DataCore;
using DataCore.EF;
using Microsoft.EntityFrameworkCore;

namespace EFMod;

/// <summary>
/// A <see cref="DbContext"/> that add entity sets based on the 
/// <see cref="IQuery{TDb, T1}"/> properties of <typeparamref name="TQueryDb"/>.
/// </summary>
/// <typeparam name="TQueryDb"></typeparam>
public class QueryDbContext<TQueryDb> : DbContext, IDbContext<TQueryDb> {
    public QueryDbContext(DbContextOptions<QueryDbContext<TQueryDb>> options) : base(options) { }
    protected QueryDbContext(DbContextOptions options) : base(options) { }

    public IQueryable<TEntity> Queryable<TEntity>()
        where TEntity : class {
        return Set<TEntity>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        foreach (var prop in typeof(TQueryDb).GetProperties()) {
            Type propertyType = prop.PropertyType;
            if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(IQuery<,>)) {
                modelBuilder.Entity(propertyType.GetGenericArguments()[1]);
            }
        }
    }
}
