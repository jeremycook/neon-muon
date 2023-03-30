namespace LoginMod;

public static class LoginConstants
{
    public static LocalLogin Unknown { get; } = new()
    {
        UserId = Guid.Empty,
        Version = 0,
        Username = "Unknown",
        Hash = string.Empty,
    };
}
