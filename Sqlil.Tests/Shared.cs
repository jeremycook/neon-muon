using System.Linq.Expressions;

namespace Sqlil.Tests;

public class Shared {

    public static LambdaExpression mapIdentity { get; } = () =>
        from user in UserContext.Users
        select user;

    // SELECT user.UserId, user.Username FROM User user
    public static LambdaExpression mapTuple { get; } = () =>
        from user in UserContext.Users
        select ValueTuple.Create(user.UserId, user.Username);

    // SELECT user.UserId, user.Username, user.IsActive FROM User user
    public static LambdaExpression mapAnon { get; } = () =>
        from user in UserContext.Users
        select new { user.UserId, user.Username, user.IsActive };

    // SELECT user.UserId FROM User user
    public static LambdaExpression mapProperty { get; } = () =>
        from user in UserContext.Users
        select user.UserId;

    // SELECT user.UserId, userRole.RoleId FROM User user JOIN UserRole userRole ON user.UserId = userRole.RoleId
    public static LambdaExpression joinAnonM { get; } = () =>
        UserContext.Users
        .Join(UserContext.UserRoles, user => user.UserId, userRole => userRole.UserId, (user, userRole) => new { user.UserId, userRole.RoleId });
    public static LambdaExpression joinAnon { get; } = () =>
        from user in UserContext.Users
        join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId
        select new { user.UserId, userRole.RoleId };

    // SELECT userRole.RoleId FROM (User) user JOIN (UserRole) userRole ON user.UserId = userRole.UserId
    public static LambdaExpression joinProp { get; } = () =>
        from user in UserContext.Users
        join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId
        select userRole.RoleId;

    // SELECT user.UserId "user.UserId", user.Username "user.Username", user.IsActive "user.IsActive", user.Created "user.Created", user.Birthday "user.Birthday", userRole.UserId "userRole.UserId", userRole.RoleId "userRole.RoleId" FROM (User) user JOIN (UserRole) userRole ON user.UserId = userRole.UserId
    public static LambdaExpression joinTuple { get; } = () =>
        from user in UserContext.Users
        join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId
        select ValueTuple.Create(user, userRole);

    // SELECT user.Username "user.Username", role.Name "role.Name" FROM (User) user JOIN (UserRole) userRole ON user.UserId = userRole.UserId
    public static LambdaExpression joinSubquery { get; } = () =>
        from user in UserContext.Users
        join role in (
            from userRole in UserContext.UserRoles
            join role in UserContext.Roles on userRole.RoleId equals role.RoleId
            select new { userRole.UserId, role.Name }
        ) on user.UserId equals role.UserId
        select new { user.Username, Role = role.Name };

    public static LambdaExpression multiJoinAnonM { get; } = () =>
        UserContext.Users
            .Join(UserContext.UserRoles, user => user.UserId, userRole => userRole.UserId, (user, userRole) => ValueTuple.Create(user, userRole))
            .Join(UserContext.Roles, ((User user, UserRole userRole) _) => _.userRole.RoleId, role => role.RoleId, ((User user, UserRole userRole) _, Role role) => new { _.user.Username, Role = role.Name });

    public static LambdaExpression multiJoinAnon { get; } = () =>
            from user in UserContext.Users
            join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId
            join role in UserContext.Roles on userRole.RoleId equals role.RoleId
            select new { user.Username, role.Name };

    //public static LambdaExpression groupJoinM = (string username) =>
    //    UserContext.Users
    //        .GroupJoin(UserContext.UserRoles, user => user.UserId, userRole => userRole.UserId, (user, userRoles) => new {
    //            user.Username,
    //            userRoles,
    //        });

    //public static LambdaExpression groupJoin = (string username) =>
    //    from user in UserContext.Users
    //    join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId into userRoleGroup
    //    select new {
    //        user.Username,
    //        userRoleGroup,
    //    };

    //public static LambdaExpression complex = (string username) =>
    //    from user in UserContext.Users
    //    where user.Username.ToLower() == username.ToLower()
    //    join userRole in UserContext.UserRoles on user.UserId equals userRole.UserId into userRoleGroup
    //    select new {
    //        user.Username,
    //        Roles = from userRole in userRoleGroup
    //                join role in UserContext.Roles on userRole.RoleId equals role.RoleId
    //                select role.Name,
    //    };
}

public sealed class UserContext {
    public static IQueryable<User> Users { get; } = null!;
    public static IQueryable<UserRole> UserRoles { get; } = null!;
    public static IQueryable<Role> Roles { get; } = null!;
}

public readonly record struct Role(Guid RoleId, string Name) {
    public static Guid Admin { get; } = Guid.Parse("0ed5476c-8cb9-44a9-9db4-8dc45990d997");
    public static Guid New { get; } = Guid.Parse("456ecbd1-c5a9-4384-9a6e-e37dbefdb630");

    public Role(string name) : this(Guid.NewGuid(), name) {
        RoleId = Guid.NewGuid();
        Name = name;
    }
}

public readonly record struct User(Guid UserId, string Username, bool IsActive, DateTime Created, DateOnly? Birthday) {
    public User(string username) : this(Guid.NewGuid(), username, true, DateTime.UtcNow, null) {
        UserId = Guid.NewGuid();
        Username = username;
    }
}

public readonly record struct UserRole(Guid UserId, Guid RoleId) { }
