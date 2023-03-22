using Microsoft.EntityFrameworkCore;

namespace LoginMod;

public class LoginServices
{
    private readonly LoginDb loginDb;
    private readonly PasswordHashing passwordHashing;

    public LoginServices(LoginDb loginDb, PasswordHashing passwordHashing)
    {
        this.loginDb = loginDb;
        this.passwordHashing = passwordHashing;
    }

    public async ValueTask<LocalLogin> Find(string username, string password, CancellationToken cancellationToken = default)
    {
        var component = await loginDb
            .LocalLogin
            .Where(x => x.Username.ToLower() == username.ToLower())
            .SingleOrDefaultAsync(cancellationToken);

        if (component is not null)
        {
            var result = passwordHashing.Verify(component.Hash, password);
            switch (result)
            {
                case PasswordHashingVerify.Success:

                    return component;

                case PasswordHashingVerify.SuccessRehashNeeded:

                    var rehashedPassword = passwordHashing.Hash(component.Hash);

                    // Rehash the password
                    _ = loginDb
                        .LocalLogin
                        .Where(x => x.EntityId == component.EntityId && x.Version == component.Version)
                        .ExecuteUpdateAsync(x => x
                            .SetProperty(o => o.Version, o => o.Version + 1)
                            .SetProperty(o => o.Hash, rehashedPassword),
                            cancellationToken);

                    return component;
            }
        }

        return LoginConstants.Unknown;
    }

    public async ValueTask<LocalLogin> Register(string username, string password, CancellationToken cancellationToken = default)
    {
        var component = await loginDb
            .LocalLogin
            .Where(x => x.Username.ToLower() == username.ToLower())
            .SingleOrDefaultAsync(cancellationToken);

        if (component is not null)
        {
            // A user with that username already exists
            return LoginConstants.Unknown;
        }

        var hashedPassword = passwordHashing.Hash(password);

        component = new()
        {
            EntityId = Guid.NewGuid(),
            Version = 0,
            Username = username,
            Hash = hashedPassword,
        };
        await loginDb.CreateAsync(component, cancellationToken);

        return component;
    }
}