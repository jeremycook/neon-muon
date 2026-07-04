using Microsoft.Data.Sqlite;
using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using System.Data.Common;
using System.Linq.Expressions;

namespace Sqlil.Sqlite.Tests;

public class SelectQueryTests {
    private static readonly SelectStmtTranslator translator = new();
    private static readonly SqliteComposer composer = new();

    private static SelectStmt Translate(LambdaExpression expression) {
        object result = translator.Translate(expression, default);
        return result switch {
            SelectStmt selectStmt => selectStmt,
            SelectCore selectCore => SelectStmt.Create(selectCore),
            _ => throw new Exception($"Unexpected translation result type: {result.GetType()}")
        };
    }

    private static string Compose(LambdaExpression expression) {
        var selectStmt = Translate(expression);
        var parameterizedSql = composer.Compose(selectStmt);
        var parameterNumber = 1;
        return string.Concat(parameterizedSql.Segments.Select(x => x switch {
            SqlRaw raw => raw.Text,
            SqlConstantParameter constant => "@p" + parameterNumber++,
            SqlInputParameter input => "@" + (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
            SqlColumn output => string.Empty,
            _ => throw new NotSupportedException(x?.GetType().ToString())
        }));
    }

    private static (string CommandText, DbCommand cmd) PrepareCommand(DbConnection connection, LambdaExpression expression) {
        var translation = Translate(expression);
        var parameterizedSql = composer.Compose(translation);
        var sqlColumns = parameterizedSql.Segments.OfType<SqlColumn>().ToStableList();
        var constantParameters = parameterizedSql.Segments.OfType<SqlConstantParameter>().ToArray();
        var inputParameters = parameterizedSql.Segments.OfType<SqlInputParameter>().ToArray();

        var parameterNumber = 1;
        var commandText = string.Concat(parameterizedSql.Segments.Select(x => x switch {
            SqlRaw raw => raw.Text,
            SqlConstantParameter constant => "@p" + parameterNumber++,
            SqlInputParameter input => "@" + (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
            SqlColumn output => string.Empty,
            _ => throw new NotSupportedException(x?.GetType().ToString())
        }));

        var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;

        parameterNumber = 1;
        foreach (var constant in constantParameters) {
            cmd.Parameters.Add(new SqliteParameter("p" + parameterNumber++, constant.Value));
        }
        foreach (var input in inputParameters) {
            cmd.Parameters.Add(new SqliteParameter(
                (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
                true));
        }

        return (commandText, cmd);
    }

    private static List<string> QueryStrings(DbConnection connection, LambdaExpression expression) {
        var (commandText, cmd) = PrepareCommand(connection, expression);
        connection.Open();
        using var reader = cmd.ExecuteReader();
        var results = new List<string>();
        while (reader.Read()) {
            results.Add(reader.GetString(0));
        }
        connection.Close();
        return results;
    }

    private static int QueryCount(DbConnection connection, LambdaExpression expression) {
        var (commandText, cmd) = PrepareCommand(connection, expression);
        connection.Open();
        var result = Convert.ToInt32(cmd.ExecuteScalar());
        connection.Close();
        return result;
    }

    [Fact]
    public void SelectUsername_ReturnsCorrectNames() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Equal(3, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Bob", usernames);
        Assert.Contains("Charlie", usernames);
    }

    [Fact]
    public void WhereActive_FiltersCorrectly() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.WhereActive());
        Assert.Equal(2, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Bob", usernames);
        Assert.DoesNotContain("Charlie", usernames);
    }

    [Fact]
    public void WhereEqualsName_ReturnsSingle() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.WhereEqualsName());
        Assert.Single(usernames);
        Assert.Equal("Alice", usernames[0]);
    }

    [Fact]
    public void WhereAnd_FiltersCorrectly() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.WhereAnd());
        Assert.Single(usernames);
        Assert.Equal("Bob", usernames[0]);
    }

    [Fact]
    public void OrderByUsername_ReturnsSorted() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.OrderByUsername());
        Assert.Equal(3, usernames.Count);
        Assert.Equal("Alice", usernames[0]);
        Assert.Equal("Bob", usernames[1]);
        Assert.Equal("Charlie", usernames[2]);
    }

    [Fact]
    public void OrderByUsernameDesc_ReturnsReverseSorted() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.OrderByUsernameDesc());
        Assert.Equal(3, usernames.Count);
        Assert.Equal("Charlie", usernames[0]);
        Assert.Equal("Bob", usernames[1]);
        Assert.Equal("Alice", usernames[2]);
    }

    [Fact]
    public void SkipTake_ReturnsPage() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.SkipTake());
        Assert.Single(usernames);
        Assert.Equal("Bob", usernames[0]);
    }

    [Fact]
    public void WhereContains_LikeQuery() {
        using var connection = DbSetup.CreateSeeded();
        var sql = Compose(TestExpressions.WhereContains());
        Assert.Contains("LIKE", sql);

        var usernames = QueryStrings(connection, TestExpressions.WhereContains());
        Assert.Equal(2, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Charlie", usernames);
    }

    [Fact]
    public void WhereToLower_LowerQuery() {
        using var connection = DbSetup.CreateSeeded();
        var sql = Compose(TestExpressions.WhereToLower());
        Assert.Contains("LOWER(", sql);

        var usernames = QueryStrings(connection, TestExpressions.WhereToLower());
        Assert.Single(usernames);
        Assert.Equal("Alice", usernames[0]);
    }

    [Fact]
    public void CountAll_ReturnsCount() {
        using var connection = DbSetup.CreateSeeded();
        var count = QueryCount(connection, TestExpressions.CountAll());
        Assert.Equal(3, count);
    }

    [Fact]
    public void MinUsername_ReturnsMin() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.MinUsername());
        Assert.Single(usernames);
        Assert.Equal("Alice", usernames[0]);
    }

    [Fact]
    public void MaxUsername_ReturnsMax() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = QueryStrings(connection, TestExpressions.MaxUsername());
        Assert.Single(usernames);
        Assert.Equal("Charlie", usernames[0]);
    }

    [Fact]
    public void JoinUserRoles_ReturnsJoinedData() {
        using var connection = DbSetup.CreateSeeded();
        var sql = Compose(TestExpressions.JoinUserRoles());
        Assert.Contains("JOIN", sql);

        var (commandText, cmd) = PrepareCommand(connection, TestExpressions.JoinUserRoles());
        connection.Open();
        using var reader = cmd.ExecuteReader();
        var results = new List<(string Username, long RoleId)>();
        while (reader.Read()) {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }
        connection.Close();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Username == "Alice");
        Assert.Contains(results, r => r.Username == "Bob");
    }
}
