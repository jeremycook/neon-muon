using DataCore;

namespace LoginMod;

public class LoginServices {
    private readonly ILoginDb loginDb;
    private readonly IQueryComposer<ILoginDb> composer;
    private readonly PasswordHashing passwordHashing;

    public LoginServices(ILoginDb loginDb, IQueryComposer<ILoginDb> composer, PasswordHashing passwordHashing) {
        this.loginDb = loginDb;
        this.composer = composer;
        this.passwordHashing = passwordHashing;
    }

    public async ValueTask<LocalLogin> Find(string username, string password, CancellationToken cancellationToken = default) {
        var login = await loginDb
            .LocalLogin
            .Filter(x => x.Username.ToLower() == username.ToLower())
            .ToOptionalAsync(composer, cancellationToken);

        if (login is null) {
            return LoginConstants.Unknown;
        }

        var result = passwordHashing.Verify(login.Hash, password);
        switch (result) {
            case PasswordHashingVerify.Success:

                return login;

            case PasswordHashingVerify.SuccessRehashNeeded:

                var rehashedPassword = passwordHashing.Hash(login.Hash);

                // Rehash the password
                _ = loginDb
                    .LocalLogin
                    .Filter(x => x.UserId == login.UserId && x.Version == login.Version)
                    .Update(x => new LocalLogin() {
                        Version = x.Version + 1,
                        Hash = rehashedPassword,
                    })
                    .ExecuteAsync(composer, cancellationToken);

                return login;

            case PasswordHashingVerify.Failed:

                return LoginConstants.Unknown;

            default:

                throw new NotSupportedException(result.ToString());
        }
    }

    public async ValueTask<LocalLogin> Register(string username, string password, CancellationToken cancellationToken = default) {
        var component = await loginDb
            .LocalLogin
            .Filter(x => x.Username.ToLower() == username.ToLower())
            .ToItemAsync(composer, cancellationToken);

        if (component is not null) {
            // A user with that username already exists
            return LoginConstants.Unknown;
        }

        var hashedPassword = passwordHashing.Hash(password);

        component = new() {
            UserId = Guid.NewGuid(),
            Version = 0,
            Username = username,
            Hash = hashedPassword,
        };

        await loginDb
            .LocalLogin
            .Insert(component)
            .ExecuteAsync(1, composer, cancellationToken);

        return component;
    }
}