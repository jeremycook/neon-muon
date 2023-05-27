using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;

namespace EFMod;

/// <summary>
/// A <see cref="DbContext"/> that add entity sets based on the 
/// <see cref="IQueryable{T}"/> properties of <typeparamref name="TContext"/>.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public abstract class QueryableDbContext<TContext> : DbContext<TContext> {

    protected QueryableDbContext(DbContextOptions options) : base(options) { }
    protected QueryableDbContext() { }

    public IQueryable<T> Queryable<T>() where T : class =>
        Set<T>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        foreach (var prop in typeof(TContext).GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)) {
            Type propertyType = prop.PropertyType;
            if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(IQueryable<>)) {
                modelBuilder.Entity(propertyType.GetGenericArguments()[0]);
            }
        }
    }

    public async Task<int> InsertAsync<T>(T entity) where T : class {
        var dbSet = Set<T>();
        dbSet.Add(entity);
        return await SaveChangesAsync();
    }

    public async Task<int> InsertRangeAsync<T>(IEnumerable<T> entities) where T : class {
        var dbSet = Set<T>();
        dbSet.AddRange(entities);
        return await SaveChangesAsync();
    }
}
