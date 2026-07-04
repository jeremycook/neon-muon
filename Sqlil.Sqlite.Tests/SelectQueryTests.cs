using Microsoft.Data.Sqlite;
using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using System.Data.Common;
using System.Linq.Expressions;

namespace Sqlil.Sqlite.Tests;

public class SelectQueryTests {

    [Fact]
    public void SelectUsername_ReturnsCorrectNames() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Equal(3, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Bob", usernames);
        Assert.Contains("Charlie", usernames);
    }

    [Fact]
    public void WhereActive_FiltersCorrectly() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.WhereActive());
        Assert.Equal(2, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Bob", usernames);
        Assert.DoesNotContain("Charlie", usernames);
    }

    [Fact]
    public void WhereEqualsName_ReturnsSingle() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.WhereEqualsName());
        Assert.Single(usernames);
        Assert.Equal("Alice", usernames[0]);
    }

    [Fact]
    public void WhereAnd_FiltersCorrectly() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.WhereAnd());
        Assert.Single(usernames);
        Assert.Equal("Bob", usernames[0]);
    }

    [Fact]
    public void OrderByUsername_ReturnsSorted() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.OrderByUsername());
        Assert.Equal(3, usernames.Count);
        Assert.Equal("Alice", usernames[0]);
        Assert.Equal("Bob", usernames[1]);
        Assert.Equal("Charlie", usernames[2]);
    }

    [Fact]
    public void OrderByUsernameDesc_ReturnsReverseSorted() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.OrderByUsernameDesc());
        Assert.Equal(3, usernames.Count);
        Assert.Equal("Charlie", usernames[0]);
        Assert.Equal("Bob", usernames[1]);
        Assert.Equal("Alice", usernames[2]);
    }

    [Fact]
    public void SkipTake_ReturnsPage() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.SkipTake());
        Assert.Single(usernames);
        Assert.Equal("Bob", usernames[0]);
    }

    [Fact]
    public void WhereContains_LikeQuery() {
        using var connection = DbSetup.CreateSeeded();
        var sql = TestHelpers.Compose(TestHelpers.Translate(TestExpressions.WhereContains()));
        Assert.Contains("LIKE", sql);

        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.WhereContains());
        Assert.Equal(2, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Charlie", usernames);
    }

    [Fact]
    public void WhereToLower_LowerQuery() {
        using var connection = DbSetup.CreateSeeded();
        var sql = TestHelpers.Compose(TestHelpers.Translate(TestExpressions.WhereToLower()));
        Assert.Contains("LOWER(", sql);

        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.WhereToLower());
        Assert.Single(usernames);
        Assert.Equal("Alice", usernames[0]);
    }

    [Fact]
    public void CountAll_ReturnsCount() {
        using var connection = DbSetup.CreateSeeded();
        var count = TestHelpers.QueryCount(connection, TestExpressions.CountAll());
        Assert.Equal(3, count);
    }

    [Fact]
    public void MinUsername_ReturnsMin() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.MinUsername());
        Assert.Single(usernames);
        Assert.Equal("Alice", usernames[0]);
    }

    [Fact]
    public void MaxUsername_ReturnsMax() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.MaxUsername());
        Assert.Single(usernames);
        Assert.Equal("Charlie", usernames[0]);
    }

    [Fact]
    public void JoinUserRoles_ReturnsJoinedData() {
        using var connection = DbSetup.CreateSeeded();
        var sql = TestHelpers.Compose(TestHelpers.Translate(TestExpressions.JoinUserRoles()));
        Assert.Contains("JOIN", sql);

        var (commandText, cmd) = TestHelpers.PrepareCommand(connection, TestExpressions.JoinUserRoles());
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
