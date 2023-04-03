using DatabaseMod.Models;
using DataCore;

namespace LoginMod;

public class LoginDb {
    public static LoginDb Instance { get; } = new();

    public IReadOnlyDatabase<LoginDb> Database { get; }
    public IQuery<LoginDb, LocalLogin> LocalLogin => new FromQuery<LoginDb, LocalLogin>();

    private LoginDb() {
        var database = new Database<LoginDb>();
        database.ContributeQueryContext();
        Database = database;
    }
}
