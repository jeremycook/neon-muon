using Microsoft.EntityFrameworkCore;

namespace DataMod.EF;

public class ComponentDbContext<T> : DbContext
{
    public ComponentDbContext(DbContextOptions<ComponentDbContext<T>> options) : base(options) { }
    protected ComponentDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var prop in typeof(T).GetProperties())
        {
            Type propertyType = prop.PropertyType;
            if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(IQueryable<>))
            {
                modelBuilder.Entity(propertyType.GetGenericArguments()[0]);
            }
        }
    }
}
