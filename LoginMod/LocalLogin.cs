using System.Security.Cryptography;

namespace LoginMod;

public sealed record LocalLogin(Guid LocalLoginId, string Username, string Hash) {

    public LocalLogin(string username) : this(
        Guid.NewGuid(),
        username,
        PasswordHashing.Instance.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)))
    ) { }

    public static LocalLogin Create(LocalLogin source) =>
        new(source.LocalLoginId, source.Username, source.Hash);
}
