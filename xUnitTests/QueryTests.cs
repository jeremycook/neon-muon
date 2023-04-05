using DatabaseMod.Models;
using DataCore;
using DataCore.Annotations;
using DataMod.Sqlite;
using Microsoft.Data.Sqlite;

namespace xUnitTests;

public class QueryTests {

    private readonly record struct Component(Guid UserId, string Username);
    private readonly record struct Dto(Guid UserId, string Username);

    private static readonly SqliteConnection Connection = CreateConnection();
    private static readonly LoginDb LoginDb = LoginDb.Instance;
    private static readonly SqliteCommandComposer<LoginDb> Composer = new(LoginDb.Database);
    private static readonly SqliteQueryRunner<LoginDb> Runner = new(Composer, new StaticDbConnectionPool<LoginDb, SqliteConnection>(Connection));
    private static readonly IReadOnlyDictionary<string, LocalLogin> Logins = new Dictionary<string, LocalLogin>() {
        { "Jeremy", new("Jeremy") },
        { "Joe", new("Joe") },
    };
    static QueryTests() {
        Runner.Execute(LoginDb
            .LocalLogin
            .InsertRange(Logins.Values));
    }

    [Fact]
    public async Task IQueryToList() {
        var list = await Runner.List(LoginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy"));

        Assert.Single(list);
        Assert.Equal(Logins["Jeremy"], list[0]);
    }

    [Fact]
    public async Task IQueryToProperty() {
        // Act

        var list = await Runner.List(LoginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => o.UserId));

        var jeremyId = list.Single();

        Assert.Equal(Logins["Jeremy"].UserId, jeremyId);
    }

    [Fact]
    public async Task IQueryToDto() {
        // Act

        var dto = await Runner.Nullable(LoginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => new Dto(o.UserId, o.Username)));

        Assert.Equal(new Dto(Logins["Jeremy"].UserId, Logins["Jeremy"].Username), dto);
    }

    [Fact]
    public async Task IQueryToTuple() {
        // Act

        var tuple = await Runner.Nullable(LoginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => ValueTuple.Create(o.UserId, o.Username)));

        Assert.Equal((Logins["Jeremy"].UserId, Logins["Jeremy"].Username), tuple);
    }
}