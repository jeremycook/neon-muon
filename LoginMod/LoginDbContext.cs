using DatabaseMod.Models;
using EFMod;
using Microsoft.EntityFrameworkCore;

namespace LoginMod;

public sealed class LoginDbContext : QueryableDbContext<LoginDbContext> {
    public IQueryable<LocalLogin> LocalLogin => Queryable<LocalLogin>();

    public LoginDbContext(DbContextOptions<LoginDbContext> options) : base(options) { }
    private LoginDbContext() { }

    public static IReadOnlyDatabase<LoginDbContext> DatabaseModel { get; }
    static LoginDbContext() {
        var database = new Database<LoginDbContext>();
        database.ContributeQueryableContext();
        DatabaseModel = database;
    }
}
