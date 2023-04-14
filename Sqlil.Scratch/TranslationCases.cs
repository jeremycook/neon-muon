using Sqlil.Core;
using Sqlil.Core.ExpressionTranslation;

namespace Sqlil.Scratch;

public static class TranslationCases {
    
    public static object Math { get; } = Lambda.Translate((int number) => (1 + number) * 3, default);

    public static object SelectIdentity { get; } = Lambda.Translate(() => UserContext
        .Users.Select(u => u)
    , default);

    public static object SelectProperty { get; } = Lambda.Translate(() => UserContext
        .Users
        .OrderByDescending(u => u.Birthday)
        .Select(user => user.Username)
        .Skip(100)
        .Take(50)
    , default);

    public static object SelectAnonymousObject { get; } = Lambda.Translate(() => UserContext
        .Users
        .OrderBy(u => u.Birthday)
        .Select(user => new { user.Username, user.Birthday })
        .Skip(100)
        .Take(50)
    , default);

    public static object Where { get; } = Lambda.Translate((bool isActive) => UserContext
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
    , default);
}
