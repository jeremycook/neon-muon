using DataCore;

namespace LoginMod;

public class LoginServices {
    private readonly ILoginDb loginDb;
    private readonly IQueryComposer<ILoginDb> orchestrator;
    private readonly PasswordHashing passwordHashing;

    public LoginServices(ILoginDb loginDb, IQueryComposer<ILoginDb> orchestrator, PasswordHashing passwordHashing) {
        this.loginDb = loginDb;
        this.orchestrator = orchestrator;
        this.passwordHashing = passwordHashing;
    }

    public async ValueTask<LocalLogin> Find(string username, string password, CancellationToken cancellationToken = default) {
        var component = await loginDb
            .LocalLogin
            .Filter(x => x.Username.ToLower() == username.ToLower())
            .ToOptionalAsync(orchestrator, cancellationToken);

        if (component is null) {
            return LoginConstants.Unknown;
        }

        var result = passwordHashing.Verify(component.Hash, password);
        switch (result) {
            case PasswordHashingVerify.Success:

                return component;

            case PasswordHashingVerify.SuccessRehashNeeded:

                var rehashedPassword = passwordHashing.Hash(component.Hash);

                // Rehash the password
                _ = loginDb
                    .LocalLogin
                    .Filter(x => x.UserId == component.UserId && x.Version == component.Version)
                    .Update(x => new LocalLogin() {
                        Version = x.Version + 1,
                        Hash = rehashedPassword,
                    })
                    .ExecuteAsync(cancellationToken);

                return component;

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
            .ToItemAsync(orchestrator, cancellationToken);

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
            .ExecuteAsync(1, cancellationToken);

        return component;
    }
}