using System.Security.Cryptography;

namespace LoginMod;

public readonly record struct LocalLogin(Guid UserId, string Username, string Hash) {

    public LocalLogin(string username) : this(
        Guid.NewGuid(),
        username,
        PasswordHashing.Instance.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)))
    ) { }

    public LocalLogin(LocalLogin source)
        : this(source.UserId, source.Username, source.Hash) { }
}
