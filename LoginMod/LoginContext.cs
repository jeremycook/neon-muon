using DatabaseMod.Models;
using DataCore;

namespace LoginMod;

public sealed class LoginContext {
    public static IQueryable<LocalLogin> LocalLogin => throw new InvalidOperationException("This is a virtual property for expression building.");

    public static IReadOnlyDatabase<LoginContext> Database { get; }

    static LoginContext() {
        var database = new Database<LoginContext>();
        database.ContributeQueryContext();
        Database = database;
    }

    private LoginContext() { throw new InvalidOperationException("Static only"); }
}
