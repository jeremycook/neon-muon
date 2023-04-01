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
        var produce = map.Produce();

        var commands = composer.Compose(produce);

        foreach (IQueryCommand<object> command in commands) {
            await command.ExecuteAsync();
            Console.WriteLine(JsonConvert.SerializeObject(command));
        }
    }

    [TestMethod]
    public async Task DataTest2() {
        var translator = new SqlExpressionTranslator();

        Expression<Func<LocalLogin, bool>> condition = o => o.Username == "jeremy";
        var sql = translator.Translate(condition);

        Console.WriteLine(sql.Preview());
        Console.WriteLine(sql.ParameterizeSql().commandText);
    }
}
