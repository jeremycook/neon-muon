using DatabaseMod.Models;
using DataCore;
using DataMod.Sqlite;
using LoginMod;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

[TestClass]
public class LoginTests {
    [TestMethod]
    public async Task RegisterAndLogin() {
        using var connection = DependencyInjector.CreateConnection();
        IDbConnectionFactory<ILoginDb> connectionFactory = new StaticDbConnectionFactory<ILoginDb>(connection);

        var loginDatabase = new Database<ILoginDb>();
        loginDatabase.ContributeQueryContext(typeof(ILoginDb));

        ILoginDb loginDb = new LoginDb();
        IQueryComposer<ILoginDb> composer = new SqliteQueryComposer<ILoginDb>(connectionFactory, loginDatabase);
        PasswordHashing passwordHashing = new();
        LoginServices loginServices = new(loginDb, composer, passwordHashing);

        var newUser = await loginServices.Register("john", "P@ssword!");
        var wrongPassword = await loginServices.Find("john", "WrongP@ssword!");
        var user = await loginServices.Find("john", "P@ssword!");

        Assert.AreNotEqual(LoginConstants.Unknown.UserId, newUser.UserId);
        Assert.AreEqual(LoginConstants.Unknown.UserId, wrongPassword.UserId);
        Assert.AreEqual(newUser.UserId, user.UserId);
    }
}