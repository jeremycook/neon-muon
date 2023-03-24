using DataMod.EF;
using Microsoft.EntityFrameworkCore;

namespace DataMod;

public class Db<TDb> where TDb : Db<TDb>
{
    public Db(ComponentDbContext<TDb> dbContext)
    {
        DbContext = dbContext;
    }

    protected ComponentDbContext<TDb> DbContext { get; }
    protected DbSet<TEntity> Set<TEntity>() where TEntity : class =>
        DbContext.Set<TEntity>();
    protected async ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await DbContext.SaveChangesAsync(cancellationToken);
}
