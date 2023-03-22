using System.ComponentModel.DataAnnotations;

namespace LoginMod;

public class LocalLogin
{
    public LocalLogin() { }
    public LocalLogin(LocalLogin source)
    {
        EntityId = source.EntityId;
        Username = source.Username;
        Hash = source.Hash;
        Version = source.Version;
    }

    [Key]
    public Guid EntityId { get; init; }

    public string Username { get; set; }

    public string Hash { get; init; }

    [ConcurrencyCheck]
    public int Version { get; set; }
}
