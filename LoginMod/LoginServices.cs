using Microsoft.EntityFrameworkCore;

namespace LoginMod;

public class LoginServices {
    private readonly LoginDbContext db;

    public LoginServices(LoginDbContext db) {
        this.db = db;
    }

    public async ValueTask<LocalLogin> Find(string username, string password, CancellationToken cancellationToken = default) {
        var loginOption = await db
            .LocalLogin
            .Where(x => x.Username.ToLower() == username.ToLower())
            .SingleOrDefaultAsync(cancellationToken);

        if (loginOption is null) {
            return LoginConstants.Unknown;
        }

        var login = loginOption;

        var result = PasswordHashing.Instance.Verify(login.Hash, password);
        switch (result) {
            case PasswordHashingVerify.Success:

                return login;

            case PasswordHashingVerify.SuccessRehashNeeded:

                var rehashedPassword = PasswordHashing.Instance.Hash(password);

                // Rehash the password
                _ = db
                    .LocalLogin
                    .Where(x => x.LocalLoginId == login.LocalLoginId)
                    .ExecuteUpdateAsync(x =>
                        x.SetProperty(o => o.Hash, rehashedPassword)
                    , cancellationToken);

                return login;

            case PasswordHashingVerify.Failed:

                return LoginConstants.Unknown;

            default:

                throw new NotSupportedException(result.ToString());
        }
    }

    public async ValueTask<LocalLogin> Register(string username, string password, CancellationToken cancellationToken = default) {
        var loginOption = await db
            .LocalLogin
            .Where(x => x.Username.ToLower() == username.ToLower())
            .SingleOrDefaultAsync(cancellationToken);

        if (loginOption is not null) {
            // A user with that username already exists
            return LoginConstants.Unknown;
        }

        var hashedPassword = PasswordHashing.Instance.Hash(password);

        var login = new LocalLogin(Guid.NewGuid(), username, hashedPassword);

        await db.InsertAsync(login);

        return login;
    }
}
