using DataMod.EF;
using LoginMod;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

[TestClass]
public class EFTests
{
    [TestMethod]
    public async Task Concurrency()
    {
        using var scope = DependencyInjector.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var loginServices = serviceProvider.GetRequiredService<LoginServices>();
        var loginDbFactory = serviceProvider.GetRequiredService<IDbContextFactory<ComponentDbContext<LoginDb>>>();

        await using var loginDb1 = await loginDbFactory.CreateDbContextAsync();
        await using var loginDb2 = await loginDbFactory.CreateDbContextAsync();

        var newUser = await loginServices.Register("john", "P@ssword!");
        var john1 = await loginDb1.Set<LocalLogin>().FindAsync(newUser.UserId);
        var john2 = await loginDb2.Set<LocalLogin>().FindAsync(newUser.UserId);

        loginDb1.Entry(john1!).Property(x => x.Version).CurrentValue++;
        loginDb1.Entry(john1!).Property(x => x.Username).CurrentValue = "john1";
        loginDb1.SaveChanges();

        loginDb2.Entry(john2!).Property(x => x.Version).CurrentValue++;
        loginDb2.Entry(john2!).Property(x => x.Username).CurrentValue = "john2";

        Assert.ThrowsException<DbUpdateConcurrencyException>(() => loginDb2.SaveChanges());
        Assert.AreEqual("john1", loginDb1.Set<LocalLogin>().Where(x => x.UserId == newUser.UserId).Select(x => x.Username).FirstOrDefault());
    }
}