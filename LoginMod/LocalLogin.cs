using System.Security.Cryptography;

namespace LoginMod;

public readonly record struct LocalLogin(Guid LocalLoginId, string Username, string Hash) {

    public LocalLogin(string username) : this(
        Guid.NewGuid(),
        username,
        PasswordHashing.Instance.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)))
    ) { }

    public LocalLogin(LocalLogin source)
        : this(source.LocalLoginId, source.Username, source.Hash) { }
}
