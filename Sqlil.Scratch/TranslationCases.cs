using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Scratch;

public static class TranslationCases {

    public static SelectStmtTranslator SelectStmtTranslator { get; set; } = new();

    public static SelectStmt TranslateToSelectStmt(LambdaExpression expression) {
        return (SelectStmt)SelectStmtTranslator.Translate(expression, default);
    }

    public static object Math { get; } = TranslateToSelectStmt((int number) => (1 + number) * 3);

    public static object SelectIdentity { get; } = TranslateToSelectStmt(() => UserContext
        .Users.Select(u => u)
    );

    public static object SelectProperty { get; } = TranslateToSelectStmt(() => UserContext
        .Users
        .OrderByDescending(u => u.Birthday)
        .Select(user => user.Username)
        .Skip(100)
        .Take(50)
    );

    public static object SelectAnonymousObject { get; } = TranslateToSelectStmt(() => UserContext
        .Users
        .OrderBy(u => u.Birthday)
        .Select(user => new { user.Username, user.Birthday })
        .Skip(100)
        .Take(50)
    );

    public static object Where { get; } = TranslateToSelectStmt((bool isActive) => UserContext
        .Users
        .Where(us => us.IsActive == isActive && (
            us.Username == "Jeremy" ||
            us.Username.StartsWith("J") ||
            us.Username.Contains("erem") ||
            us.Username.EndsWith("y")
        ))
        .OrderByDescending(u => u.Birthday)
        .Select(user => new { user.Username, Disabled = !user.IsActive })
        .Skip(100)
        .Take(50)
    );
}
