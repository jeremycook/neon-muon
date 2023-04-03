using DatabaseMod.Models;
using DataCore;
using DataMod.Sqlite;
using LoginMod;
using Microsoft.Data.Sqlite;

namespace UnitTests;

[TestClass]
public class LoginTests {
    [TestMethod]
    public async Task RegisterAndLogin() {
        LoginDb loginDb = LoginDb.Instance;

        var composer = new SqliteCommandComposer<LoginDb>(loginDb.Database);

        using var connection = DependencyInjector.CreateConnection();
        IDbConnectionPool<LoginDb, SqliteConnection> connectionPool = new StaticDbConnectionPool<LoginDb, SqliteConnection>(connection);

        IQueryRunner<LoginDb> runner = new SqliteQueryRunner<LoginDb>(composer, connectionPool);

        LoginServices loginServices = new(loginDb, runner);

        var newUser = await loginServices.Register("john", "P@ssword!");
        var wrongPassword = await loginServices.Find("john", "WrongP@ssword!");
        var user = await loginServices.Find("john", "P@ssword!");

        Assert.AreNotEqual(LoginConstants.Unknown.UserId, newUser.UserId);
        Assert.AreEqual(LoginConstants.Unknown.UserId, wrongPassword.UserId);
        Assert.AreEqual(newUser.UserId, user.UserId);
    }
}