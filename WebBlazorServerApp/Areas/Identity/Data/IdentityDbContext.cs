using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace WebBlazorServerApp.Areas.Identity.Data;

public partial class IdentityDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext {
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder builder) {
        builder.HasDefaultSchema("identity");
        base.OnModelCreating(builder);
        foreach (var et in builder.Model.GetEntityTypes()) {
            var newTableName = AspNetPrefixPattern().Replace(et.GetTableName() ?? throw new NullReferenceException(), "");
            et.SetTableName(newTableName);
        }
    }

    [GeneratedRegex("^AspNet")]
    private static partial Regex AspNetPrefixPattern();
}
