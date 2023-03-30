using DataCore;
using DataCore.EF;

namespace LoginMod;

public interface ILoginDb : IDb<ILoginDb>
{
    IQuery<ILoginDb, LocalLogin> LocalLogin { get; }
}

public class LoginDb : Db<ILoginDb>, ILoginDb
{
    public LoginDb(IComponentDbContext<ILoginDb> dbContext) : base(dbContext)
    {
    }

    public IQuery<ILoginDb, LocalLogin> LocalLogin => From<LocalLogin>();

    public async ValueTask CreateAsync(LocalLogin localLogin, CancellationToken cancellationToken)
    {
        await LocalLogin
            .Insert(localLogin)
            .ExecuteAsync(cancellationToken);
    }
}
