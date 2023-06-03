using Microsoft.EntityFrameworkCore;

namespace LoginMod;

public class LoginServices {
    private readonly LoginDbContext db;

    public LoginServices(LoginDbContext db) {
        this.db = db;
    }

    public sealed record FindLoginRecord(Guid LocalLoginId, string Username, string[] Roles) { }

    public async ValueTask<FindLoginRecord> Find(string username, string password, CancellationToken cancellationToken = default) {
        var loginOption = await db
            .LocalLogin
            .Where(x => x.Username.ToLower() == username.ToLower())
            .Select(x => new {
                x.LocalLoginId,
                x.Username,
                x.Hash,
                Roles = db.LoginRoles
                    .Where(r => r.LocalLoginId == x.LocalLoginId)
                    .OrderBy(x => x.Role)
                    .Select(x => x.Role)
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (loginOption is null) {
            return new FindLoginRecord(
                LoginConstants.Unknown.LocalLoginId,
                LoginConstants.Unknown.Username,
                Array.Empty<string>()
            );
        }

        var login = loginOption;

        var result = PasswordHashing.Instance.Verify(login.Hash, password);
        switch (result) {
            case PasswordHashingVerify.Success:

                return new(login.LocalLoginId, login.Username, login.Roles);

            case PasswordHashingVerify.SuccessRehashNeeded:

                var rehashedPassword = PasswordHashing.Instance.Hash(password);

                // Rehash the password
                _ = db
                    .LocalLogin
                    .Where(x => x.LocalLoginId == login.LocalLoginId)
                    .ExecuteUpdateAsync(x =>
                        x.SetProperty(o => o.Hash, rehashedPassword)
                    , cancellationToken);

                return new(login.LocalLoginId, login.Username, login.Roles);

            case PasswordHashingVerify.Failed:

                return new FindLoginRecord(
                    LoginConstants.Unknown.LocalLoginId,
                    LoginConstants.Unknown.Username,
                    Array.Empty<string>()
                );

            default:

                throw new NotSupportedException(result.ToString());
        }
    }

    /// <summary>
    /// Returns errors or an empty array.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<List<string>> Register(string username, string password, CancellationToken cancellationToken = default) {

        var errors = new List<string>();

        if (username.Length < 3) {
            errors.Add("The username is must be at least 3 characters long.");
        }
        if (password.Length < 10) {
            errors.Add("The password must be at least 10 characters long.");
        }

        if (errors.Any()) {
            return errors;
        }

        var loginOption = await db
            .LocalLogin
            .Where(x => x.Username.ToLower() == username.ToLower())
            .SingleOrDefaultAsync(cancellationToken);

        if (loginOption != null) {
            errors.Add("The username has been taken");
            return errors;
        }

        if (errors.Any()) {
            return errors;
        }

        // Create the user
        var hashedPassword = PasswordHashing.Instance.Hash(password);
        var login = new LocalLogin(Guid.NewGuid(), username, hashedPassword);
        await db.InsertAsync(login);

        return errors;
    }
}
