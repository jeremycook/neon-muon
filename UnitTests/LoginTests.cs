using DataMod.EF;
using LoginMod;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

[TestClass]
public class LoginTests
{
    [TestMethod]
    public async Task RegisterAndLogin()
    {
        using var scope = DependencyInjector.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var loginServices = serviceProvider.GetRequiredService<LoginServices>();

        var newUser = await loginServices.Register("john", "P@ssword!");
        var wrongPassword = await loginServices.Find("john", "WrongP@ssword!");
        var user = await loginServices.Find("john", "P@ssword!");

        Assert.AreNotEqual(LoginConstants.Unknown.EntityId, newUser.EntityId);
        Assert.AreEqual(LoginConstants.Unknown.EntityId, wrongPassword.EntityId);
        Assert.AreEqual(newUser.EntityId, user.EntityId);
    }

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
        var john1 = await loginDb1.Set<LocalLogin>().FindAsync(newUser.EntityId);
        var john2 = await loginDb2.Set<LocalLogin>().FindAsync(newUser.EntityId);

        john1!.Version++;
        john1.Username = "john1";
        loginDb1.SaveChanges();

        john2!.Version++;
        john2.Username = "john2";

        Assert.ThrowsException<DbUpdateConcurrencyException>(() => loginDb2.SaveChanges());
        Assert.AreEqual("john1", loginDb1.Set<LocalLogin>().Where(x => x.EntityId == newUser.EntityId).Select(x => x.Username).FirstOrDefault());
    }
}