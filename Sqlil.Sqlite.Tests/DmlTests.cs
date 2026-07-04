using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using System.Data.Common;
using System.Linq.Expressions;

namespace Sqlil.Sqlite.Tests;

public class DmlTests {
    private static readonly SelectStmtTranslator translator = new();
    private static readonly SqliteComposer composer = new();

    private static object Translate(LambdaExpression expression) {
        return translator.Translate(expression, default);
    }

    private static string Compose(object statement) {
        var parameterizedSql = composer.Compose(statement);
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
            cmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("p" + parameterNumber++, constant.Value ?? DBNull.Value));
        }
        foreach (var input in inputParameters) {
            cmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter(
                (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
                true));
        }

        return (commandText, cmd);
    }

    private static int ExecuteDml(DbConnection connection, LambdaExpression expression) {
        var (commandText, cmd) = PrepareCommand(connection, expression);
        if (connection.State != System.Data.ConnectionState.Open) {
            connection.Open();
        }
        var result = cmd.ExecuteNonQuery();
        return result;
    }

    private static List<string> QueryStrings(DbConnection connection, LambdaExpression expression) {
        var (commandText, cmd) = PrepareCommand(connection, expression);
        if (connection.State != System.Data.ConnectionState.Open) {
            connection.Open();
        }
        using var reader = cmd.ExecuteReader();
        var results = new List<string>();
        while (reader.Read()) {
            results.Add(reader.GetString(0));
        }
        return results;
    }

    #region INSERT Tests

    [Fact]
    public void Insert_TranslatesToInsertStmt() {
        var stmt = Translate(TestExpressions.InsertEve());
        Assert.IsType<InsertStmt>(stmt);
    }

    [Fact]
    public void Insert_GeneratesCorrectSql() {
        var stmt = Translate(TestExpressions.InsertEve());
        var sql = Compose(stmt);
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("VALUES", sql);
    }

    [Fact]
    public void Insert_ExeutesSuccessfully() {
        using var connection = DbSetup.CreateSeeded();
        var affected = ExecuteDml(connection, TestExpressions.InsertEve());
        Assert.Equal(1, affected);

        // Verify the row was inserted
        var usernames = QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Contains("Eve", usernames);
        Assert.Equal(4, usernames.Count);
    }

    [Fact]
    public void Insert_MultipleRows() {
        using var connection = DbSetup.CreateSeeded();
        var affected1 = ExecuteDml(connection, TestExpressions.InsertEve());
        var affected2 = ExecuteDml(connection, TestExpressions.InsertDave());
        Assert.Equal(1, affected1);
        Assert.Equal(1, affected2);

        var usernames = QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Equal(5, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Dave", usernames);
    }

    #endregion

    #region UPDATE Tests

    [Fact]
    public void Update_TranslatesToUpdateStmt() {
        var stmt = Translate(TestExpressions.UpdateUsername());
        Assert.IsType<UpdateStmt>(stmt);
    }

    [Fact]
    public void Update_GeneratesCorrectSql() {
        var stmt = Translate(TestExpressions.UpdateUsername());
        var sql = Compose(stmt);
        Assert.Contains("UPDATE", sql);
        Assert.Contains("SET", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void Update_WithWhere_UpdatesCorrectRow() {
        using var connection = DbSetup.CreateSeeded();
        var affected = ExecuteDml(connection, TestExpressions.UpdateUsername());
        Assert.Equal(1, affected);

        // Verify Alice was renamed to ALICE
        var usernames = QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Contains("ALICE", usernames);
        Assert.DoesNotContain("Alice", usernames);
    }

    [Fact]
    public void Update_WithoutWhere_UpdatesAllRows() {
        using var connection = DbSetup.CreateSeeded();
        var affected = ExecuteDml(connection, TestExpressions.UpdateDeactivate());
        Assert.Equal(3, affected);

        // Verify all users are inactive
        var activeUsers = QueryStrings(connection, TestExpressions.WhereActive());
        Assert.Empty(activeUsers);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public void Delete_TranslatesToDeleteStmt() {
        var stmt = Translate(TestExpressions.DeleteInactive());
        Assert.IsType<DeleteStmt>(stmt);
    }

    [Fact]
    public void Delete_GeneratesCorrectSql() {
        var stmt = Translate(TestExpressions.DeleteInactive());
        var sql = Compose(stmt);
        Assert.Contains("DELETE FROM", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void Delete_WithWhere_DeletesCorrectRows() {
        using var connection = DbSetup.CreateSeeded();
        var affected = ExecuteDml(connection, TestExpressions.DeleteInactive());
        Assert.Equal(1, affected); // Charlie is inactive

        // Verify only active users remain
        var usernames = QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Equal(2, usernames.Count);
        Assert.Contains("Alice", usernames);
        Assert.Contains("Bob", usernames);
        Assert.DoesNotContain("Charlie", usernames);
    }

    [Fact]
    public void Delete_WithoutWhere_DeletesAllRows() {
        using var connection = DbSetup.CreateSeeded();
        // Disable FK constraints for this test (must be set on open connection)
        if (connection.State != System.Data.ConnectionState.Open) {
            connection.Open();
        }
        using (var cmd = connection.CreateCommand()) {
            cmd.CommandText = "PRAGMA foreign_keys = OFF";
            cmd.ExecuteNonQuery();
        }

        var affected = ExecuteDml(connection, TestExpressions.DeleteAll());
        Assert.Equal(3, affected);

        var usernames = QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Empty(usernames);
    }

    #endregion
}
