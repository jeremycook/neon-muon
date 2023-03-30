using DataCore;
using DataCore.EF;
using Microsoft.EntityFrameworkCore;

namespace DataMod.EF;

public class ComponentDbContext<T> : DbContext, IComponentDbContext<T>
{
    public ComponentDbContext(DbContextOptions<ComponentDbContext<T>> options) : base(options) { }
    protected ComponentDbContext(DbContextOptions options) : base(options) { }

    public IQueryable<TEntity> Queryable<TEntity>()
        where TEntity : class
    {
        return Set<TEntity>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var prop in typeof(T).GetProperties())
        {
            Type propertyType = prop.PropertyType;
            if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(IQuery<,>))
            {
                modelBuilder.Entity(propertyType.GetGenericArguments()[1]);
            }
        }
    }
}
