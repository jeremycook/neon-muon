using Sqlil.Core.ExpressionTranslation;

namespace Sqlil.Scratch;

public static class TranslationCases {

    public static object Math { get; } = SelectStmtTranslator.ConvertToSelectStmt((int number) => (1 + number) * 3);

    public static object SelectIdentity { get; } = SelectStmtTranslator.ConvertToSelectStmt(() => UserContext
        .Users.Select(u => u)
    );

    public static object SelectProperty { get; } = SelectStmtTranslator.ConvertToSelectStmt(() => UserContext
        .Users
        .OrderByDescending(u => u.Birthday)
        .Select(user => user.Username)
        .Skip(100)
        .Take(50)
    );

    public static object SelectAnonymousObject { get; } = SelectStmtTranslator.ConvertToSelectStmt(() => UserContext
        .Users
        .OrderBy(u => u.Birthday)
        .Select(user => new { user.Username, user.Birthday })
        .Skip(100)
        .Take(50)
    );

    public static object Where { get; } = SelectStmtTranslator.ConvertToSelectStmt((bool isActive) => UserContext
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
