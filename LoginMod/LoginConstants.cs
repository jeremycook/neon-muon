using System.Security.Cryptography;

namespace LoginMod;

public static class LoginConstants {
    public static LocalLogin Unknown { get; } = new(Guid.Empty, "Unknown", Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)));
}
