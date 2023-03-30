using System.ComponentModel.DataAnnotations;

namespace LoginMod;

public class LocalLogin
{
    public LocalLogin() { }
    public LocalLogin(LocalLogin source)
    {
        UserId = source.UserId;
        Username = source.Username;
        Hash = source.Hash;
        Version = source.Version;
    }

    [Key]
    public Guid UserId { get; init; }

    [ConcurrencyCheck]
    public int Version { get; init; }

    public string Username { get; init; }

    public string Hash { get; init; }
}
