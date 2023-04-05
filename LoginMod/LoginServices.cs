using DataCore;

namespace LoginMod;

public class LoginServices {
    private readonly IQueryRunner<LoginContext> Runner;

    public LoginServices(IQueryRunner<LoginContext> runner) {
        Runner = runner;
    }

    public async ValueTask<LocalLogin> Find(string username, string password, CancellationToken cancellationToken = default) {
        var loginOption = await Runner.Nullable(LoginContext
            .LocalLogin
            .Filter(x => x.Username.ToLower() == username.ToLower()),
            cancellationToken);

        if (!loginOption.HasValue) {
            return LoginConstants.Unknown;
        }

        var login = loginOption.Value;

        var result = PasswordHashing.Instance.Verify(login.Hash, password);
        switch (result) {
            case PasswordHashingVerify.Success:

                return login;

            case PasswordHashingVerify.SuccessRehashNeeded:

                var rehashedPassword = PasswordHashing.Instance.Hash(login.Hash);

                // Rehash the password
                _ = Runner.Execute(LoginContext
                    .LocalLogin
                    .Filter(x => x.UserId == login.UserId && x.Version == login.Version)
                    .Update(x => new LocalLogin(x) {
                        Version = x.Version + 1,
                        Hash = rehashedPassword,
                    }),
                    cancellationToken);

                return login;

            case PasswordHashingVerify.Failed:

                return LoginConstants.Unknown;

            default:

                throw new NotSupportedException(result.ToString());
        }
    }

    public async ValueTask<LocalLogin> Register(string username, string password, CancellationToken cancellationToken = default) {
        var loginOption = await Runner.Nullable(LoginContext
            .LocalLogin
            .Filter(x => x.Username.ToLower() == username.ToLower()),
            cancellationToken);

        if (loginOption.HasValue) {
            // A user with that username already exists
            return LoginConstants.Unknown;
        }

        var hashedPassword = PasswordHashing.Instance.Hash(password);

        var login = new LocalLogin(Guid.NewGuid(), 0, username, hashedPassword);

        await Runner.Execute(LoginContext
            .LocalLogin
            .Insert(login),
            cancellationToken);

        return login;
    }
}