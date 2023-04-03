using DataCore;
using DataMod.Sqlite;
using LoginMod;
using System.Linq.Expressions;

namespace UnitTests;

[TestClass]
public class SqliteTests {
    readonly record struct Dto(Guid UserId, string Username);

    [TestMethod]
    public void TranslateCondition() {
        var translator = new SqlExpressionTranslator();

        var username = "Jeremy";
        Expression<Func<LocalLogin, bool>> condition = o => o.Username.ToLower() == username;
        var sql = translator.Translate(condition);

        Assert.AreEqual("(lower(\"LocalLogin\".\"Username\") = @p0)", sql.ParameterizeSql().CommandText);
    }

    [TestMethod]
    public void TranslateMapProperty() {
        var translator = new SqlExpressionTranslator();

        Expression<Func<LocalLogin, Guid>> mapping = o => o.UserId;
        var sql = translator.Translate(mapping);

        Assert.AreEqual("\"LocalLogin\".\"UserId\"", sql.ParameterizeSql().CommandText);
    }

    [TestMethod]
    public void TranslateMapValueTupleCreate() {
        var translator = new SqlExpressionTranslator();

        Expression<Func<LocalLogin, ValueTuple<Guid, string>>> map = o => ValueTuple.Create(o.UserId, o.Username);
        var sql = translator.Translate(map);

        Console.WriteLine(sql.ParameterizeSql().CommandText);
    }

    [TestMethod]
    public void TranslateMapNewStruct() {
        var translator = new SqlExpressionTranslator();

        Expression<Func<LocalLogin, Dto>> mapping = o => new Dto(o.UserId, o.Username);
        var sql = translator.Translate(mapping);

        Console.WriteLine(sql.ParameterizeSql().CommandText);
    }

}
