using DataCore;
using Sqlil.Core;
using Sqlil.Core.Db;
using System.Data.Common;

namespace LoginMod;

public class LoginServices {
    private readonly DbConnection connection;

    public LoginServices(DbConnection connection) {
        this.connection = connection;
    }

    public async ValueTask<LocalLogin> Find(string username, string password, CancellationToken cancellationToken = default) {
        var loginOption = await connection.Nullable(() => LoginContext
            .LocalLogin
            .Where(x => x.Username.ToLower() == username.ToLower())
            .Select(x => x),
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

                var rehashedPassword = PasswordHashing.Instance.Hash(password);

                // Rehash the password
                _ = connection.Execute(() => LoginContext
                    .LocalLogin
                    .Where(x => x.LocalLoginId == login.LocalLoginId)
                    .Update(x => new(x) {
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
        var loginOption = await connection.Nullable(() => LoginContext
            .LocalLogin
            .Where(x => x.Username.ToLower() == username.ToLower())
            .Select(x => x),
        cancellationToken);

        if (loginOption.HasValue) {
            // A user with that username already exists
            return LoginConstants.Unknown;
        }

        var hashedPassword = PasswordHashing.Instance.Hash(password);

        var login = new LocalLogin(Guid.NewGuid(), username, hashedPassword);

        await connection.Execute(() => LoginContext
            .LocalLogin
            .Insert(login),
        cancellationToken);

        return login;
    }
}