using DataCore;

namespace LoginMod;

public interface ILoginDb {
    IQuery<ILoginDb, LocalLogin> LocalLogin { get; }
}

public class LoginDb : ILoginDb {
    public LoginDb(IQueryOrchestrator<ILoginDb> handler) {
        Handler = handler;
    }

    public IQuery<ILoginDb, LocalLogin> LocalLogin => new FromQuery<ILoginDb, LocalLogin>();

    public IQueryOrchestrator<ILoginDb> Handler { get; }
}
