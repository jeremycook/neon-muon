using DataCore;

namespace LoginMod;

public interface ILoginDb {
    IQuery<ILoginDb, LocalLogin> LocalLogin { get; }
}

public class LoginDb : ILoginDb {
    public IQuery<ILoginDb, LocalLogin> LocalLogin => new FromQuery<ILoginDb, LocalLogin>();
}
