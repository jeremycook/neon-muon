using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LoginMod;

public class PasswordHashing
{
    private static PasswordHasher<object> PasswordHasher { get; } = new(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions()
    {
        // See https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
        IterationCount = 600_000,
    }));

    public string Hash(string password)
    {
        return PasswordHasher.HashPassword(null!, password);
    }

    public PasswordHashingVerify Verify(string hashedPassword, string password)
    {
        var passwordVerificationResult = PasswordHasher.VerifyHashedPassword(null!, hashedPassword, password);
        return passwordVerificationResult switch
        {
            PasswordVerificationResult.Failed => PasswordHashingVerify.Failed,
            PasswordVerificationResult.Success => PasswordHashingVerify.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordHashingVerify.SuccessRehashNeeded,
            _ => throw new NotSupportedException(passwordVerificationResult.ToString()),
        };
    }
}
