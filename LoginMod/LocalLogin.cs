using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace LoginMod;

public readonly record struct LocalLogin {
    [Obsolete]
    public LocalLogin() { }

    public LocalLogin(string username) {
        UserId = Guid.NewGuid();
        Version = 0;
        Username = username;
        Hash = PasswordHashing.Instance.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)));
    }

    public LocalLogin(Guid userId, int version, string username, string hash) {
        UserId = userId;
        Version = version;
        Username = username;
        Hash = hash;
    }

    public LocalLogin(LocalLogin source)
        : this(source.UserId, source.Version, source.Username, source.Hash) { }

    [Key]
    public Guid UserId { get; init; }

    [ConcurrencyCheck]
    public int Version { get; init; }

    public string Username { get; init; }

    public string Hash { get; init; }
}
