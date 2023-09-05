using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;

namespace NeonMS.Security;

public static class SCRAMSHA256
{
    /// <summary>
    /// See SCRAM_SHA_256_NAME at https://github.com/postgres/postgres/blob/master/src/include/common/scram-common.h
    /// </summary>
    const string SCRAM_SHA_256_NAME = "SCRAM-SHA-256";

    /// <summary>
    /// See SCRAM_DEFAULT_SALT_LEN at https://github.com/postgres/postgres/blob/master/src/include/common/scram-common.h
    /// </summary>
    const int SCRAM_DEFAULT_SALT_LEN = 16;

    /// <summary>
    /// See PG_SHA256_DIGEST_LENGTH at https://github.com/postgres/postgres/blob/master/src/include/common/sha2.h
    /// </summary>
    const int PG_SHA256_DIGEST_LENGTH = 32;

    /// <summary>
    /// See SCRAM_DEFAULT_ITERATIONS at https://github.com/postgres/postgres/blob/master/src/include/common/scram-common.h
    /// </summary>
    const int SCRAM_DEFAULT_ITERATIONS = 4096;

    static readonly byte[] CLIENT_KEY = Encoding.UTF8.GetBytes("Client Key");
    static readonly byte[] SERVER_KEY = Encoding.UTF8.GetBytes("Server Key");

    public static string EncryptPassword(string password, int minLength = 22)
    {
        if (password is null ||
            password.Where(ch => !char.IsWhiteSpace(ch)).Count() < minLength)
        {
            throw new ArgumentException($"The password must contain at least {minLength} non-whitespace characters.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SCRAM_DEFAULT_SALT_LEN);

        var digestKey = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, SCRAM_DEFAULT_ITERATIONS, PG_SHA256_DIGEST_LENGTH);

        var clientKey = HMACSHA256.HashData(digestKey, CLIENT_KEY);

        var storedKey = SHA256.HashData(clientKey);

        var serverKey = HMACSHA256.HashData(digestKey, SERVER_KEY);

        return $"{SCRAM_SHA_256_NAME}${SCRAM_DEFAULT_ITERATIONS}:{Convert.ToBase64String(salt)}${Convert.ToBase64String(storedKey)}:{Convert.ToBase64String(serverKey)}";
    }
}
