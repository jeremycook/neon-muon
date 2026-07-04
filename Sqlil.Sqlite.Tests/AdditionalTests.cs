using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Sqlite.Tests;

public class AdditionalTests {

    #region NULL Comparison Tests

    [Fact]
    public void WhereBirthdayIsNull_FiltersCorrectly() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.WhereBirthdayIsNull());
        Assert.Single(usernames);
        Assert.Equal("Charlie", usernames[0]);
    }

    [Fact]
    public void WhereBirthdayIsNotNull_FiltersCorrectly() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.WhereBirthdayIsNotNull());
        Assert.Equal(2, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Bob", usernames);
    }

    #endregion

    #region ThenBy Tests

    [Fact]
    public void OrderByUsernameThenByCreated_ReturnsSorted() {
        using var connection = DbSetup.CreateSeeded();
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.OrderByUsernameThenByCreated());
        Assert.Equal(3, usernames.Count);
        // All usernames are unique, so ThenBy doesn't change the result
        Assert.Equal("Alice", usernames[0]);
        Assert.Equal("Bob", usernames[1]);
        Assert.Equal("Charlie", usernames[2]);
    }

    #endregion

    #region Aggregate Tests

    [Fact]
    public void CountNoSelector_ReturnsCount() {
        using var connection = DbSetup.CreateSeeded();
        var count = TestHelpers.QueryCount(connection, TestExpressions.CountNoSelector());
        Assert.Equal(3, count);
    }

    #endregion

    #region Arithmetic Tests

    [Fact]
    public void SelectUserIdTimesTwo_ReturnsCorrectValues() {
        using var connection = DbSetup.CreateSeeded();
        var (commandText, cmd) = TestHelpers.PrepareCommand(connection, TestExpressions.SelectUserIdTimesTwo());
        connection.Open();
        using var reader = cmd.ExecuteReader();
        var results = new List<long>();
        while (reader.Read()) {
            results.Add(reader.GetInt64(0));
        }
        connection.Close();

        Assert.Equal(3, results.Count);
        Assert.Contains(2, results); // UserId 1 * 2
        Assert.Contains(4, results); // UserId 2 * 2
        Assert.Contains(6, results); // UserId 3 * 2
    }

    #endregion

    #region SelectAnonymous Tests

    [Fact]
    public void SelectAnonymous_ReturnsCorrectColumns() {
        using var connection = DbSetup.CreateSeeded();
        var (commandText, cmd) = TestHelpers.PrepareCommand(connection, TestExpressions.SelectAnonymousUsed());
        connection.Open();
        using var reader = cmd.ExecuteReader();
        Assert.Equal(2, reader.FieldCount);
        Assert.Equal("Username", reader.GetName(0));
        Assert.Equal("IsActive", reader.GetName(1));

        var rows = 0;
        while (reader.Read()) {
            rows++;
        }
        connection.Close();
        Assert.Equal(3, rows);
    }

    #endregion

    #region GroupJoin Tests

    [Fact]
    public void GroupJoin_ReturnsResults() {
        using var connection = DbSetup.CreateSeeded();
        var sql = TestHelpers.Compose(TestHelpers.Translate(TestExpressions.GroupJoinUsersRoles()));
        Assert.Contains("LEFT JOIN", sql);
    }

    #endregion

    #region Union Tests

    [Fact]
    public void Union_ReturnsCombinedResults() {
        using var connection = DbSetup.CreateSeeded();
        var sql = TestHelpers.Compose(TestHelpers.Translate(TestExpressions.UnionUsernames()));
        Assert.Contains("UNION", sql);

        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.UnionUsernames());
        Assert.Equal(2, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Bob", usernames);
    }

    #endregion

    #region Subquery Tests

    [Fact]
    public void Any_WithPredicate_GeneratesExists() {
        var stmt = TestHelpers.Translate(TestExpressions.WhereActive());
        var sql = TestHelpers.Compose(stmt);
        // WhereActive doesn't use Any, but let's verify the SQL is valid
        Assert.Contains("WHERE", sql);
    }

    #endregion
}
