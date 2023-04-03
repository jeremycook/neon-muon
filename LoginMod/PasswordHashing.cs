using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LoginMod;

public class PasswordHashing {
    public static PasswordHashing Instance => _singleton;

    private PasswordHashing() { }

    public string Hash(string password) {
        return PasswordHasher.HashPassword(null!, password);
    }

    public PasswordHashingVerify Verify(string hashedPassword, string password) {
        var passwordVerificationResult = PasswordHasher.VerifyHashedPassword(null!, hashedPassword, password);
        return passwordVerificationResult switch {
            PasswordVerificationResult.Failed => PasswordHashingVerify.Failed,
            PasswordVerificationResult.Success => PasswordHashingVerify.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordHashingVerify.SuccessRehashNeeded,
            _ => throw new NotSupportedException(passwordVerificationResult.ToString()),
        };
    }

    private static readonly PasswordHashing _singleton = new();

    private static PasswordHasher<object> PasswordHasher { get; } = new(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions() {
        // See https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
        IterationCount = 600_000,
    }));

}
