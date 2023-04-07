using DatabaseMod.Models;
using DataCore;

namespace LoginMod;

public sealed class LoginContext {
    public static IQuery<LoginContext, LocalLogin> LocalLogin => new FromQuery<LoginContext, LocalLogin>();

    public static IReadOnlyDatabase<LoginContext> Database { get; }
    static LoginContext() {
        var database = new Database<LoginContext>();
        database.ContributeQueryContext();
        Database = database;
    }

    private LoginContext() { throw new InvalidOperationException("Static only"); }
}
