using System.Linq.Expressions;
using Sqlil.Core;

namespace Sqlil.Sqlite.Tests;

public static class TestUserContext {
    public static IQueryable<User> Users { get; } = null!;
    public static IQueryable<UserRole> UserRoles { get; } = null!;
    public static IQueryable<Role> Roles { get; } = null!;
}

public static class TestExpressions {
    // SELECT expressions
    public static Expression<Func<IQueryable<string>>> SelectUsername() =>
        () => TestUserContext.Users.Select(u => u.Username);

    public static Expression<Func<IQueryable<long>>> SelectUserId() =>
        () => TestUserContext.Users.Select(u => u.UserId);

    public static Expression<Func<IQueryable<object>>> SelectAnonymous() =>
        () => TestUserContext.Users.Select(u => new { u.UserId, u.Username });

    // WHERE expressions
    public static Expression<Func<IQueryable<string>>> WhereActive() =>
        () => TestUserContext.Users.Where(u => u.IsActive).Select(u => u.Username);

    public static Expression<Func<IQueryable<string>>> WhereEqualsName() =>
        () => TestUserContext.Users.Where(u => u.Username == "Alice").Select(u => u.Username);

    public static Expression<Func<IQueryable<string>>> WhereAnd() =>
        () => TestUserContext.Users.Where(u => u.IsActive && u.Username == "Bob").Select(u => u.Username);

    // ORDER BY expressions
    public static Expression<Func<IQueryable<string>>> OrderByUsername() =>
        () => TestUserContext.Users.OrderBy(u => u.Username).Select(u => u.Username);

    public static Expression<Func<IQueryable<string>>> OrderByUsernameDesc() =>
        () => TestUserContext.Users.OrderByDescending(u => u.Username).Select(u => u.Username);

    // SKIP/TAKE expressions
    public static Expression<Func<IQueryable<string>>> SkipTake() =>
        () => TestUserContext.Users.OrderBy(u => u.Username).Skip(1).Take(1).Select(u => u.Username);

    // LIKE expressions
    public static Expression<Func<IQueryable<string>>> WhereContains() =>
        () => TestUserContext.Users.Where(u => u.Username.Contains("li")).Select(u => u.Username);

    public static Expression<Func<IQueryable<string>>> WhereToLower() =>
        () => TestUserContext.Users.Where(u => u.Username.ToLower() == "alice").Select(u => u.Username);

    // AGGREGATE expressions
    public static Expression<Func<IQueryable<object>>> JoinUserRoles() =>
        () => TestUserContext.Users
            .Join(TestUserContext.UserRoles, u => u.UserId, ur => ur.UserId, (u, ur) => new { u.Username, ur.RoleId });

    public static Expression<Func<int>> CountAll() =>
        () => TestUserContext.Users.Count();

    public static Expression<Func<string>> MinUsername() =>
        () => TestUserContext.Users.Min(u => u.Username);

    public static Expression<Func<string>> MaxUsername() =>
        () => TestUserContext.Users.Max(u => u.Username);

    // INSERT expressions
    public static Expression<Func<IQueryable<User>>> InsertEve() =>
        () => TestUserContext.Users.Insert(new User(4, "Eve", true, new DateTime(2024, 1, 1), new DateOnly(1990, 1, 15)));

    public static Expression<Func<IQueryable<User>>> InsertDave() =>
        () => TestUserContext.Users.Insert(new User(5, "Dave", false, new DateTime(2024, 6, 15), null));

    // UPDATE expressions
    public static Expression<Func<IQueryable<User>>> UpdateUsername() =>
        () => TestUserContext.Users
            .Where(u => u.Username == "Alice")
            .Update(u => new User(u.UserId, "ALICE", u.IsActive, u.Created, u.Birthday));

    public static Expression<Func<IQueryable<User>>> UpdateDeactivate() =>
        () => TestUserContext.Users
            .Update(u => new User(u.UserId, u.Username, false, u.Created, u.Birthday));

    // DELETE expressions
    public static Expression<Func<IQueryable<User>>> DeleteInactive() =>
        () => TestUserContext.Users
            .Where(u => !u.IsActive)
            .Delete();

    public static Expression<Func<IQueryable<User>>> DeleteAll() =>
        () => TestUserContext.Users.Delete();
}

public readonly record struct User(long UserId, string Username, bool IsActive, DateTime Created, DateOnly? Birthday);
public readonly record struct UserRole(long UserId, Guid RoleId);
public readonly record struct Role(Guid RoleId, string Name);
