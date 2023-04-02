using DatabaseMod.Models;
using DataCore;
using DataMod.Sqlite;
using LoginMod;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace UnitTests;

[TestClass]
public class DataTests {
    [TestMethod]
    public async Task DataTest1() {
        var translator = new SqlExpressionTranslator();

        Expression<Func<LocalLogin, bool>> condition = o => o.Username == "jeremy";
        var sql = translator.Translate(condition);

        Console.WriteLine(sql.Preview());
        Console.WriteLine(sql.ParameterizeSql().commandText);
    }

    [TestMethod]
    public async Task DataTest2() {
        using var connection = DependencyInjector.CreateConnection();
        IDbConnectionFactory<ILoginDb> connectionFactory = new StaticDbConnectionFactory<ILoginDb>(connection);

        var loginDatabase = new Database<ILoginDb>();
        loginDatabase.ContributeQueryContext(typeof(ILoginDb));

        IQueryComposer<ILoginDb> composer = new SqliteQueryComposer<ILoginDb>(connectionFactory, loginDatabase);

        ILoginDb loginDb = new LoginDb();

        // Act

        var from = loginDb.LocalLogin;
        var filter = from.Filter(o => o.Username == "Jeremy");
        var map = filter.Map(o => o);

        var commands = composer.Compose(map);

        foreach (IQueryCommand command in commands) {
            await command.ExecuteAsync();
            Console.WriteLine(JsonConvert.SerializeObject(command));
        }
    }

    [TestMethod]
    public async Task IQueryToList() {
        using var connection = DependencyInjector.CreateConnection();
        IDbConnectionFactory<ILoginDb> connectionFactory = new StaticDbConnectionFactory<ILoginDb>(connection);

        var loginDatabase = new Database<ILoginDb>();
        loginDatabase.ContributeQueryContext(typeof(ILoginDb));

        ILoginDb loginDb = new LoginDb();
        IQueryComposer<ILoginDb> composer = new SqliteQueryComposer<ILoginDb>(connectionFactory, loginDatabase);

        // Act

        var localLogins = await loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => o)
            .ToListAsync(composer);
        Console.WriteLine(JsonConvert.SerializeObject(localLogins));
    }

    [TestMethod]
    public async Task IQueryToOptional() {
        using var connection = DependencyInjector.CreateConnection();
        IDbConnectionFactory<ILoginDb> connectionFactory = new StaticDbConnectionFactory<ILoginDb>(connection);

        var loginDatabase = new Database<ILoginDb>();
        loginDatabase.ContributeQueryContext(typeof(ILoginDb));

        ILoginDb loginDb = new LoginDb();
        IQueryComposer<ILoginDb> composer = new SqliteQueryComposer<ILoginDb>(connectionFactory, loginDatabase);

        // Act

        var localLogin = await loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => o)
            .ToOptionalAsync(composer);
        Console.WriteLine(JsonConvert.SerializeObject(localLogin));
    }

    [TestMethod]
    public async Task IQueryToItem() {
        using var connection = DependencyInjector.CreateConnection();
        IDbConnectionFactory<ILoginDb> connectionFactory = new StaticDbConnectionFactory<ILoginDb>(connection);

        var loginDatabase = new Database<ILoginDb>();
        loginDatabase.ContributeQueryContext(typeof(ILoginDb));

        ILoginDb loginDb = new LoginDb();
        IQueryComposer<ILoginDb> composer = new SqliteQueryComposer<ILoginDb>(connectionFactory, loginDatabase);

        // Act

        var insertedLogins = new LocalLogin[] {
            new() { UserId = Guid.NewGuid(), Username = "Jeremy" },
            new() { UserId = Guid.NewGuid(), Username = "Joe" }
        };
        var numberOfLoginsInserted = await loginDb
            .LocalLogin
            .InsertRange(insertedLogins)
            .ExecuteAsync(composer);

        var fetchedLogin = await loginDb
            .LocalLogin
            .Filter(o => o.Username == "Jeremy")
            .Map(o => o)
            .ToItemAsync(composer);

        Assert.IsTrue(insertedLogins.Contains(fetchedLogin));
    }
}
