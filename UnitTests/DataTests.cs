using DataCore;
using DataMod.Sqlite;
using LoginMod;
using Microsoft.Data.Sqlite;

namespace UnitTests;

[TestClass]
public class DataTests {

    private readonly record struct Dto(Guid UserId, string Username);

    private readonly IReadOnlyDictionary<string, LocalLogin> Logins = new Dictionary<string, LocalLogin>() {
        { "Jeremy", new("Jeremy") },
        { "Joe", new("Joe") },
    };

    private (LoginDb, IQueryRunner<LoginDb>) Arrange() {
        var loginDb = LoginDb.Instance;

        var composer = new SqliteCommandComposer<LoginDb>(loginDb.Database);

        var connection = DependencyInjector.CreateConnection();
        var runner = new SqliteQueryRunner<LoginDb>(composer, new StaticDbConnectionPool<LoginDb, SqliteConnection>(connection));

        runner.Execute(loginDb
            .LocalLogin
            .InsertRange(Logins.Values));

        return (loginDb, runner);
    }

    [TestMethod]
    public async Task IQueryToList() {
        var (loginDb, runner) = Arrange();

        var list = await runner.List(loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy"));

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(new LocalLogin(Logins["Jeremy"]), list[0]);
    }

    [TestMethod]
    public async Task IQueryToProperty() {
        var (loginDb, runner) = Arrange();

        // Act

        var list = await runner.List(loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => o.UserId));

        var jeremyId = list.Single();

        Assert.AreEqual(Logins["Jeremy"].UserId, jeremyId);
    }

    [TestMethod]
    public async Task IQueryToDto() {
        var (loginDb, runner) = Arrange();

        // Act

        var dto = await runner.Nullable(loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => new Dto(o.UserId, o.Username)));

        Assert.AreEqual(new Dto(Logins["Jeremy"].UserId, Logins["Jeremy"].Username), dto);
    }

    [TestMethod]
    public async Task IQueryToTuple() {
        var (loginDb, runner) = Arrange();

        // Act

        var tuple = await runner.Nullable(loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => ValueTuple.Create(o.UserId, o.Username)));

        Assert.AreEqual((Logins["Jeremy"].UserId, Logins["Jeremy"].Username), tuple);
    }
}
