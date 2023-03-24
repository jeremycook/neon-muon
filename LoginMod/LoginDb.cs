using DataMod;
using DataMod.EF;
using Microsoft.EntityFrameworkCore;

namespace LoginMod;

public class LoginDb : Db<LoginDb>
{
    public LoginDb(ComponentDbContext<LoginDb> dbContext) : base(dbContext)
    {
    }

    public IQueryable<LocalLogin> LocalLogin => Set<LocalLogin>().AsNoTracking();

    public async ValueTask CreateAsync(LocalLogin component, CancellationToken cancellationToken)
    {
        Set<LocalLogin>().Add(component);
        await SaveChangesAsync(cancellationToken);
    }
}
