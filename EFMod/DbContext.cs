using Microsoft.EntityFrameworkCore;

namespace EFMod;

public abstract class DbContext<TContext> : DbContext {
    public DbContext(DbContextOptions options) : base(options) { }
    protected DbContext() { }
}
