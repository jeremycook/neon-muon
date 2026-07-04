using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using System.Data.Common;
using System.Linq.Expressions;

namespace Sqlil.Sqlite.Tests;

public class DmlTests {

    #region INSERT Tests

    [Fact]
    public void Insert_TranslatesToInsertStmt() {
        var stmt = TestHelpers.Translate(TestExpressions.InsertEve());
        Assert.IsType<InsertStmt>(stmt);
    }

    [Fact]
    public void Insert_GeneratesCorrectSql() {
        var stmt = TestHelpers.Translate(TestExpressions.InsertEve());
        var sql = TestHelpers.Compose(stmt);
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("VALUES", sql);
    }

    [Fact]
    public void Insert_ExeutesSuccessfully() {
        using var connection = DbSetup.CreateSeeded();
        var affected = TestHelpers.ExecuteDml(connection, TestExpressions.InsertEve());
        Assert.Equal(1, affected);

        // Verify the row was inserted
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Contains("Eve", usernames);
        Assert.Equal(4, usernames.Count);
    }

    [Fact]
    public void Insert_MultipleRows() {
        using var connection = DbSetup.CreateSeeded();
        var affected1 = TestHelpers.ExecuteDml(connection, TestExpressions.InsertEve());
        var affected2 = TestHelpers.ExecuteDml(connection, TestExpressions.InsertDave());
        Assert.Equal(1, affected1);
        Assert.Equal(1, affected2);

        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Equal(5, usernames.Count);
        Assert.Contains("Eve", usernames);
        Assert.Contains("Dave", usernames);
    }

    #endregion

    #region UPDATE Tests

    [Fact]
    public void Update_TranslatesToUpdateStmt() {
        var stmt = TestHelpers.Translate(TestExpressions.UpdateUsername());
        Assert.IsType<UpdateStmt>(stmt);
    }

    [Fact]
    public void Update_GeneratesCorrectSql() {
        var stmt = TestHelpers.Translate(TestExpressions.UpdateUsername());
        var sql = TestHelpers.Compose(stmt);
        Assert.Contains("UPDATE", sql);
        Assert.Contains("SET", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void Update_WithWhere_UpdatesCorrectRow() {
        using var connection = DbSetup.CreateSeeded();
        var affected = TestHelpers.ExecuteDml(connection, TestExpressions.UpdateUsername());
        Assert.Equal(1, affected);

        // Verify Alice was renamed to ALICE
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Contains("ALICE", usernames);
        Assert.DoesNotContain("Alice", usernames);
    }

    [Fact]
    public void Update_WithoutWhere_UpdatesAllRows() {
        using var connection = DbSetup.CreateSeeded();
        var affected = TestHelpers.ExecuteDml(connection, TestExpressions.UpdateDeactivate());
        Assert.Equal(3, affected);

        // Verify all users are inactive
        var activeUsers = TestHelpers.QueryStrings(connection, TestExpressions.WhereActive());
        Assert.Empty(activeUsers);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public void Delete_TranslatesToDeleteStmt() {
        var stmt = TestHelpers.Translate(TestExpressions.DeleteInactive());
        Assert.IsType<DeleteStmt>(stmt);
    }

    [Fact]
    public void Delete_GeneratesCorrectSql() {
        var stmt = TestHelpers.Translate(TestExpressions.DeleteInactive());
        var sql = TestHelpers.Compose(stmt);
        Assert.Contains("DELETE FROM", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void Delete_WithWhere_DeletesCorrectRows() {
        using var connection = DbSetup.CreateSeeded();
        var affected = TestHelpers.ExecuteDml(connection, TestExpressions.DeleteInactive());
        Assert.Equal(1, affected); // Charlie is inactive

        // Verify only active users remain
        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.SelectUsername());
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

        var affected = TestHelpers.ExecuteDml(connection, TestExpressions.DeleteAll());
        Assert.Equal(3, affected);

        var usernames = TestHelpers.QueryStrings(connection, TestExpressions.SelectUsername());
        Assert.Empty(usernames);
    }

    #endregion

    #region RETURNING Clause Tests

    [Fact]
    public void InsertWithReturning_GeneratesCorrectSql() {
        // Manually construct an InsertStmt with RETURNING
        var stmt = InsertStmt.Create(
            TableName.Create("User", typeof(User)),
            StableList.Create(ColumnName.Create("Username", typeof(string))),
            StableList.Create(StableList.Create<Expr>(ExprBindConstant.Create(typeof(string), "Frank"))),
            Returning: StableList.Create<ResultColumn>(
                ResultColumnExpr.Create(ExprColumn.Create(ColumnName.Create("UserId", typeof(long)))),
                ResultColumnExpr.Create(ExprColumn.Create(ColumnName.Create("Username", typeof(string))))
            )
        );
        var sql = TestHelpers.Compose(stmt);
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("RETURNING", sql);
        Assert.Contains("UserId", sql);
        Assert.Contains("Username", sql);
    }

    [Fact]
    public void DeleteWithReturning_GeneratesCorrectSql() {
        var stmt = DeleteStmt.Create(
            TableName.Create("User", typeof(User)),
            Where: ExprBinary.Create(BinaryOperator.Equal,
                ExprColumn.Create(ColumnName.Create("IsActive", typeof(bool))),
                ExprBindConstant.Create(typeof(bool), false)),
            Returning: StableList.Create<ResultColumn>(
                ResultColumnExpr.Create(ExprColumn.Create(ColumnName.Create("UserId", typeof(long))))
            )
        );
        var sql = TestHelpers.Compose(stmt);
        Assert.Contains("DELETE FROM", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("RETURNING", sql);
        Assert.Contains("UserId", sql);
    }

    [Fact]
    public void UpdateWithReturning_GeneratesCorrectSql() {
        var stmt = UpdateStmt.Create(
            TableName.Create("User", typeof(User)),
            StableList.Create((ColumnName.Create("Username", typeof(string)), (Expr)ExprBindConstant.Create(typeof(string), "Frank"))),
            Where: ExprBinary.Create(BinaryOperator.Equal,
                ExprColumn.Create(ColumnName.Create("UserId", typeof(long))),
                ExprBindConstant.Create(typeof(long), 1)),
            Returning: StableList.Create<ResultColumn>(
                ResultColumnExpr.Create(ExprColumn.Create(ColumnName.Create("Username", typeof(string))))
            )
        );
        var sql = TestHelpers.Compose(stmt);
        Assert.Contains("UPDATE", sql);
        Assert.Contains("SET", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("RETURNING", sql);
    }

    [Fact]
    public void InsertWithOnConflictNothing_GeneratesCorrectSql() {
        var stmt = InsertStmt.Create(
            TableName.Create("User", typeof(User)),
            StableList.Create(ColumnName.Create("Username", typeof(string))),
            StableList.Create(StableList.Create<Expr>(ExprBindConstant.Create(typeof(string), "Frank"))),
            OnConflict: new OnConflict(
                StableList.Create(ColumnName.Create("Username", typeof(string))),
                new OnConflictNothing()
            )
        );
        var sql = TestHelpers.Compose(stmt);
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("ON CONFLICT", sql);
        Assert.Contains("DO NOTHING", sql);
    }

    [Fact]
    public void InsertWithOnConflictUpdate_GeneratesCorrectSql() {
        var stmt = InsertStmt.Create(
            TableName.Create("User", typeof(User)),
            StableList.Create(ColumnName.Create("Username", typeof(string)), ColumnName.Create("IsActive", typeof(bool))),
            StableList.Create(StableList.Create<Expr>(ExprBindConstant.Create(typeof(string), "Frank"), ExprBindConstant.Create(typeof(bool), true))),
            OnConflict: new OnConflict(
                StableList.Create(ColumnName.Create("Username", typeof(string))),
                new OnConflictUpdate(
                    StableList.Create((ColumnName.Create("IsActive", typeof(bool)), (Expr)ExprBindConstant.Create(typeof(bool), true)))
                )
            )
        );
        var sql = TestHelpers.Compose(stmt);
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("ON CONFLICT", sql);
        Assert.Contains("DO UPDATE SET", sql);
        Assert.Contains("IsActive", sql);
    }

    #endregion
}
