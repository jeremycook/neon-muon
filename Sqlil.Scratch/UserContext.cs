namespace Sqlil.Scratch;

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
