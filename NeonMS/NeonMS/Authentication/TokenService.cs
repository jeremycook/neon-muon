using Microsoft.IdentityModel.Tokens;
using NeonMS.Security;
using System.IdentityModel.Tokens.Jwt;

namespace NeonMS.Authentication;

public class TokenService
{
    public static string CreateToken(Keys keys, DateTime expires, IDictionary<string, object> claims)
    {
        if (expires > DateTime.UtcNow.AddDays(90))
        {
            throw new ArgumentException($"The {nameof(expires)} argument is more than 90 days in the future.", nameof(expires));
        }

        SecurityTokenDescriptor tokenDescriptor = CreateTokenDescriptor(keys, expires, claims);
        JwtSecurityTokenHandler tokenHandler = new();
        SecurityToken securityToken = tokenHandler.CreateToken(tokenDescriptor);
        string token = tokenHandler.WriteToken(securityToken);

        return token;
    }

    private static SecurityTokenDescriptor CreateTokenDescriptor(Keys keys, DateTime expires, IDictionary<string, object> claims)
    {
        SecurityTokenDescriptor tokenDescriptor = new()
        {
            NotBefore = DateTime.UtcNow.AddSeconds(-30),
            Expires = expires,
            SigningCredentials = new SigningCredentials(keys.SigningKey, SecurityAlgorithms.HmacSha256Signature),
            EncryptingCredentials = new EncryptingCredentials(keys.EncryptingKey, SecurityAlgorithms.Aes128KW, SecurityAlgorithms.Aes128CbcHmacSha256),
            Claims = claims,
        };

        return tokenDescriptor;
    }
}
