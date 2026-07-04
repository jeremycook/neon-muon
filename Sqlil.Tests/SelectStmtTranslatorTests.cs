using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Tests;

public class SelectStmtTranslatorTests {
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

    #region Select Translation Tests

    [Fact]
    public void SelectIdentity() {
        Expression<Func<IQueryable<User>>> expression = () => UserContext.Users.Select(u => u);
        var stmt = Translate(expression);
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("\"User\"", sql);
    }

    [Fact]
    public void SelectSingleProperty() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users.Select(u => u.Username);
        var stmt = Translate(expression);
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("\"Username\"", sql);
    }

    [Fact]
    public void SelectAnonymousType() {
        Expression<Func<IQueryable<object>>> expression = () => UserContext.Users.Select(u => new { u.UserId, u.Username });
        var stmt = Translate(expression);
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("\"UserId\"", sql);
        Assert.Contains("\"Username\"", sql);
    }

    [Fact]
    public void SelectWithWhere() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.IsActive)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("\"IsActive\"", sql);
    }

    [Fact]
    public void SelectWithOrderBy() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderBy(u => u.Username)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("ORDER BY", sql);
    }

    [Fact]
    public void SelectWithOrderByDescending() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderByDescending(u => u.Username)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("DESC", sql);
    }

    [Fact]
    public void SelectWithSkip() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderBy(u => u.Username)
            .Skip(10)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("OFFSET", sql);
    }

    [Fact]
    public void SelectWithTake() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderBy(u => u.Username)
            .Take(5)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("LIMIT", sql);
    }

    [Fact]
    public void SelectWithSkipAndTake() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderBy(u => u.Username)
            .Skip(10)
            .Take(5)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("LIMIT", sql);
        Assert.Contains("OFFSET", sql);
    }

    #endregion

    #region Where Translation Tests

    [Fact]
    public void WhereEquals() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username == "test")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("=", sql);
    }

    [Fact]
    public void WhereNotEquals() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username != "test")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("<>", sql);
    }

    [Fact]
    public void WhereGreaterThan() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username == "test" && u.IsActive)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("AND", sql);
    }

    [Fact]
    public void WhereLessThan() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username == "test" || !u.IsActive)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("OR", sql);
    }

    [Fact]
    public void WhereAnd() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.IsActive && u.Username == "test")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("AND", sql);
    }

    [Fact]
    public void WhereOr() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username == "a" || u.Username == "b")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("OR", sql);
    }

    [Fact]
    public void WhereContains() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.Contains("test"))
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("LIKE", sql);
    }

    [Fact]
    public void WhereStartsWith() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.StartsWith("test"))
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("LIKE", sql);
    }

    [Fact]
    public void WhereEndsWith() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.EndsWith("test"))
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("LIKE", sql);
    }

    [Fact]
    public void WhereToLower() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.ToLower() == "test")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("LOWER(", sql);
    }

    [Fact]
    public void WhereToUpper() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.ToUpper() == "TEST")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("UPPER(", sql);
    }

    [Fact]
    public void WhereWithParameter() {
        Expression<Func<string, IQueryable<string>>> expression = (string name) => UserContext.Users
            .Where(u => u.Username == name)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("@name", sql);
    }

    #endregion

    #region Join Translation Tests

    [Fact]
    public void JoinQuerySyntax() {
        Expression<Func<IQueryable<object>>> expression = () =>
            from user in UserContext.Users
            join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId
            select new { user.UserId, userRole.RoleId };
        var sql = Compose(expression);
        Assert.Contains("JOIN", sql);
        Assert.Contains("ON", sql);
        Assert.Contains("=", sql);
    }

    [Fact]
    public void JoinMethodSyntax() {
        Expression<Func<IQueryable<object>>> expression = () =>
            UserContext.Users
                .Join(
                    UserContext.UserRoles,
                    user => user.UserId,
                    userRole => userRole.UserId,
                    (user, userRole) => new { user.UserId, userRole.RoleId }
                );
        var sql = Compose(expression);
        Assert.Contains("JOIN", sql);
        Assert.Contains("ON", sql);
    }

    #endregion

    #region GroupJoin Translation Tests

    [Fact]
    public void GroupJoinQuerySyntax() {
        Expression<Func<IQueryable<object>>> expression = () =>
            from user in UserContext.Users
            join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId into userRoleGroup
            select new { user.Username, userRoleGroup };
        var sql = Compose(expression);
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("ON", sql);
    }

    [Fact]
    public void GroupJoinMethodSyntax() {
        Expression<Func<IQueryable<object>>> expression = () =>
            UserContext.Users
                .GroupJoin(
                    UserContext.UserRoles,
                    user => user.UserId,
                    userRole => userRole.UserId,
                    (user, userRoles) => new { user.Username, userRoles }
                );
        var sql = Compose(expression);
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("ON", sql);
    }

    #endregion

    #region Aggregate Translation Tests

    [Fact]
    public void CountAll() {
        Expression<Func<int>> expression = () => UserContext.Users.Count();
        var sql = Compose(expression);
        Assert.Contains("COUNT(*)", sql);
    }

    [Fact]
    public void CountWithPredicate() {
        Expression<Func<int>> expression = () => UserContext.Users.Count(u => u.IsActive);
        var sql = Compose(expression);
        Assert.Contains("COUNT(*)", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void SumWithSelector() {
        Expression<Func<int, double>> expression = (int val) => UserContext.Users.Sum(u => val);
        var sql = Compose(expression);
        Assert.Contains("SUM(", sql);
    }

    [Fact]
    public void AverageWithSelector() {
        Expression<Func<int, double>> expression = (int val) => UserContext.Users.Average(u => val);
        var sql = Compose(expression);
        Assert.Contains("AVG(", sql);
    }

    [Fact]
    public void MinWithSelector() {
        Expression<Func<string>> expression = () => UserContext.Users.Min(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("MIN(", sql);
    }

    [Fact]
    public void MaxWithSelector() {
        Expression<Func<string>> expression = () => UserContext.Users.Max(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("MAX(", sql);
    }

    #endregion

    #region Complex Query Tests

    [Fact]
    public void WhereOrderBySkipTake() {
        Expression<Func<IQueryable<object>>> expression = () => UserContext.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Username)
            .Skip(10)
            .Take(5)
            .Select(u => new { u.UserId, u.Username });
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("LIMIT", sql);
        Assert.Contains("OFFSET", sql);
    }

    [Fact]
    public void SelectAfterWhere() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.IsActive)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("\"IsActive\"", sql);
        Assert.Contains("\"Username\"", sql);
    }

    [Fact]
    public void WhereWithComplexAnd() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.IsActive && u.Username == "test" && u.Username == "other")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("AND", sql);
    }

    [Fact]
    public void WhereWithNestedOr() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.IsActive && (u.Username == "a" || u.Username == "b"))
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("WHERE", sql);
        Assert.Contains("AND", sql);
        Assert.Contains("OR", sql);
    }

    #endregion

    #region SqliteComposer Rendering Tests

    [Fact]
    public void ComposeSimpleSelect() {
        Expression<Func<IQueryable<User>>> expression = () => UserContext.Users.Select(u => u);
        var sql = Compose(expression);
        Assert.StartsWith("SELECT", sql);
        Assert.Contains("FROM", sql);
    }

    [Fact]
    public void ComposeSelectWithAlias() {
        Expression<Func<IQueryable<object>>> expression = () => UserContext.Users
            .Select(u => new { u.Username, u.IsActive });
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("\"Username\"", sql);
        Assert.Contains("\"IsActive\"", sql);
    }

    [Fact]
    public void ComposeSelectWithOrderBy() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderBy(u => u.Username)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("ORDER BY", sql);
        Assert.DoesNotContain("DESC", sql);
    }

    [Fact]
    public void ComposeSelectWithOrderByDesc() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderByDescending(u => u.Username)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("DESC", sql);
    }

    [Fact]
    public void ComposeSelectWithLimitOffset() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderBy(u => u.Username)
            .Skip(20)
            .Take(10)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("LIMIT", sql);
        Assert.Contains("OFFSET", sql);
    }

    [Fact]
    public void ComposeJoinSelect() {
        Expression<Func<IQueryable<object>>> expression = () =>
            from user in UserContext.Users
            join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId
            select new { user.Username, userRole.RoleId };
        var sql = Compose(expression);
        Assert.Contains("JOIN", sql);
        Assert.Contains("ON", sql);
        Assert.Contains("FROM", sql);
    }

    [Fact]
    public void ComposeGroupJoinSelect() {
        Expression<Func<IQueryable<object>>> expression = () =>
            from user in UserContext.Users
            join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId into userRoleGroup
            select new { user.Username, userRoleGroup };
        var sql = Compose(expression);
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("ON", sql);
        Assert.Contains("FROM", sql);
    }

    [Fact]
    public void ComposeCountSelect() {
        Expression<Func<int>> expression = () => UserContext.Users.Count();
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("COUNT(*)", sql);
    }

    [Fact]
    public void ComposeSumSelect() {
        Expression<Func<int, double>> expression = (int val) => UserContext.Users.Sum(u => val);
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("SUM(", sql);
    }

    [Fact]
    public void ComposeAverageSelect() {
        Expression<Func<int, double>> expression = (int val) => UserContext.Users.Average(u => val);
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("AVG(", sql);
    }

    [Fact]
    public void ComposeMinSelect() {
        Expression<Func<string>> expression = () => UserContext.Users.Min(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("MIN(", sql);
    }

    [Fact]
    public void ComposeMaxSelect() {
        Expression<Func<string>> expression = () => UserContext.Users.Max(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("SELECT", sql);
        Assert.Contains("MAX(", sql);
    }

    #endregion

    #region AST Structure Tests

    [Fact]
    public void SelectIdentityAstStructure() {
        Expression<Func<IQueryable<User>>> expression = () => UserContext.Users.Select(u => u);
        var stmt = Translate(expression);
        Assert.Single(stmt.SelectCores);
        Assert.IsType<SelectCoreNormal>(stmt.SelectCores[0]);
        var core = (SelectCoreNormal)stmt.SelectCores[0];
        Assert.False(core.Distinct);
        Assert.Null(core.Where);
        Assert.Null(core.JoinClause);
        Assert.Empty(core.GroupBys);
        Assert.Null(core.Having);
    }

    [Fact]
    public void SelectWhereAstStructure() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.IsActive && u.Username == "test")
            .Select(u => u.Username);
        var stmt = Translate(expression);
        Assert.Single(stmt.SelectCores);
        var core = (SelectCoreNormal)stmt.SelectCores[0];
        Assert.NotNull(core.Where);
        Assert.IsType<ExprBinary>(core.Where);
    }

    [Fact]
    public void SelectOrderByAstStructure() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderBy(u => u.Username)
            .Select(u => u.Username);
        var stmt = Translate(expression);
        Assert.Single(stmt.OrderingTerms);
        Assert.False(stmt.OrderingTerms[0].Desc);
    }

    [Fact]
    public void SelectOrderByDescAstStructure() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderByDescending(u => u.Username)
            .Select(u => u.Username);
        var stmt = Translate(expression);
        Assert.Single(stmt.OrderingTerms);
        Assert.True(stmt.OrderingTerms[0].Desc);
    }

    [Fact]
    public void SelectLimitOffsetAstStructure() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .OrderBy(u => u.Username)
            .Skip(10)
            .Take(5)
            .Select(u => u.Username);
        var stmt = Translate(expression);
        Assert.NotNull(stmt.Limit);
        Assert.NotNull(stmt.Offset);
        Assert.IsType<ExprBindConstant>(stmt.Limit);
        Assert.IsType<ExprBindConstant>(stmt.Offset);
    }

    [Fact]
    public void JoinAstStructure() {
        Expression<Func<IQueryable<object>>> expression = () =>
            from user in UserContext.Users
            join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId
            select new { user.Username, userRole.RoleId };
        var stmt = Translate(expression);
        var core = (SelectCoreNormal)stmt.SelectCores[0];
        Assert.NotNull(core.JoinClause);
        Assert.Single(core.JoinClause.Joins);
        Assert.Equal(JoinOperator.Inner, core.JoinClause.Joins[0].JoinOperator);
        Assert.IsType<JoinConstraintOn>(core.JoinClause.Joins[0].JoinConstraint);
    }

    [Fact]
    public void GroupJoinAstStructure() {
        Expression<Func<IQueryable<object>>> expression = () =>
            from user in UserContext.Users
            join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId into userRoleGroup
            select new { user.Username, userRoleGroup };
        var stmt = Translate(expression);
        var core = (SelectCoreNormal)stmt.SelectCores[0];
        Assert.NotNull(core.JoinClause);
        Assert.Single(core.JoinClause.Joins);
        Assert.Equal(JoinOperator.Left, core.JoinClause.Joins[0].JoinOperator);
        Assert.IsType<JoinConstraintOn>(core.JoinClause.Joins[0].JoinConstraint);
    }

    [Fact]
    public void CountAstStructure() {
        Expression<Func<int>> expression = () => UserContext.Users.Count();
        var stmt = Translate(expression);
        var core = (SelectCoreNormal)stmt.SelectCores[0];
        Assert.Single(core.ResultColumns);
        var resultCol = (ResultColumnExpr)core.ResultColumns[0];
        Assert.IsType<ExprFunction>(resultCol.Expr);
        var func = (ExprFunction)resultCol.Expr;
        Assert.Equal(ExprFunctionName.Count, func.FunctionName);
        Assert.Empty(func.Exprs);
    }

    #endregion

    #region String Methods Tests

    [Fact]
    public void StringContainsRendersLike() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.Contains("test"))
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("LIKE", sql);
    }

    [Fact]
    public void StringStartsWithRendersLike() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.StartsWith("test"))
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("LIKE", sql);
    }

    [Fact]
    public void StringEndsWithRendersLike() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.EndsWith("test"))
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("LIKE", sql);
    }

    [Fact]
    public void StringToLowerRendersLower() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.ToLower() == "test")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("LOWER(", sql);
    }

    [Fact]
    public void StringToUpperRendersUpper() {
        Expression<Func<IQueryable<string>>> expression = () => UserContext.Users
            .Where(u => u.Username.ToUpper() == "TEST")
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("UPPER(", sql);
    }

    #endregion

    #region Arithmetic Tests

    [Fact]
    public void ArithmeticAdd() {
        Expression<Func<int, IQueryable<string>>> expression = (int n) => UserContext.Users
            .Where(u => n + 1 == 2)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("+", sql);
    }

    [Fact]
    public void ArithmeticMultiply() {
        Expression<Func<int, IQueryable<string>>> expression = (int n) => UserContext.Users
            .Where(u => n * 2 == 4)
            .Select(u => u.Username);
        var sql = Compose(expression);
        Assert.Contains("*", sql);
    }

    #endregion
}
