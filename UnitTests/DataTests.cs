using DatabaseMod.Models;
using DataCore;
using DataMod.Sqlite;
using LoginMod;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace UnitTests;

[TestClass]
public class DataTests {

    private readonly record struct Dto(Guid UserId, string Username);

    private readonly IReadOnlyDictionary<string, LocalLogin> NewLogins = new Dictionary<string, LocalLogin>() {
        { "Jeremy", new("Jeremy") },
        { "Joe", new("Joe") },
    };

    private (LoginDb, IQueryRunner<LoginDb>) Arrange() {
        var loginDb = LoginDb.Instance;

        var loginDatabase = new Database<LoginDb>();
        loginDatabase.ContributeQueryContext();
        var composer = new SqliteCommandComposer<LoginDb>(loginDatabase);

        var connection = DependencyInjector.CreateConnection();
        var runner = new SqliteQueryRunner<LoginDb>(composer, new StaticDbConnectionPool<LoginDb, SqliteConnection>(connection));

        runner.Execute(loginDb
            .LocalLogin
            .InsertRange(NewLogins.Values));

        return (loginDb, runner);
    }

    [TestMethod]
    public async Task IQueryToList() {
        var (loginDb, runner) = Arrange();

        var list = await runner.List(loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy"));

        Console.WriteLine(JsonConvert.SerializeObject(list));
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

        // Assert

        Assert.AreEqual(NewLogins["Jeremy"].UserId, jeremyId);
    }

    [TestMethod]
    public async Task IQueryToDto() {
        var (loginDb, runner) = Arrange();

        // Act

        var dto = await runner.Nullable(loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => new Dto(o.UserId, o.Username)));

        Console.WriteLine(JsonConvert.SerializeObject(dto));
    }
}
