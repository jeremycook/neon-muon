using LoginMod;
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

        Assert.AreNotEqual(LoginConstants.Unknown.UserId, newUser.UserId);
        Assert.AreEqual(LoginConstants.Unknown.UserId, wrongPassword.UserId);
        Assert.AreEqual(newUser.UserId, user.UserId);
    }
}