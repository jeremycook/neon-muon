using NeonMS.DataAccess;
using NeonMS.Security;
using Npgsql;

namespace NeonMS.Authentication;

internal class AuthHelpers
{
    internal static async Task<int>
    CreateLogin(
        DB DB,
        DataCredential credential,
        DateTime validUntil,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(credential.Username)) { throw new ArgumentException("The value is empty.", $"{nameof(credential)}.{nameof(credential.Username)}"); }
        if (string.IsNullOrWhiteSpace(credential.Password)) { throw new ArgumentException("The value is empty.", $"{nameof(credential)}.{nameof(credential.Password)}"); }
        if (string.IsNullOrWhiteSpace(credential.Role)) { throw new ArgumentException("The value is empty.", $"{nameof(credential)}.{nameof(credential.Role)}"); }

        var newLoginIdentifier = Quote.Identifier(credential.Username);
        var newPasswordLiteral = Quote.Literal(SCRAMSHA256.EncryptPassword(credential.Password));
        var validUntilLiteral = Quote.Literal(validUntil);
        var grantedRoleIdentifier = Quote.Identifier(credential.Role);

        using var maintenance = await DB.MaintenanceConnection(credential.Server, cancellationToken);
        using var batch = new NpgsqlBatch(maintenance)
        {
            BatchCommands = {
                new("CALL public.drop_expired_logins()"),
                new($"""
                CREATE ROLE {newLoginIdentifier} WITH
                    LOGIN
                    NOSUPERUSER
                    NOCREATEDB
                    NOCREATEROLE
                    INHERIT
                    NOREPLICATION
                    CONNECTION LIMIT -1
                    IN ROLE {grantedRoleIdentifier}
                    VALID UNTIL {validUntilLiteral}
                    ENCRYPTED PASSWORD {newPasswordLiteral}
                """),
                new($"ALTER ROLE {newLoginIdentifier} SET role TO {grantedRoleIdentifier}"),
            }
        };

        return await batch.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static async Task<int>
    RenewLogin(
        DB DB,
        DataCredential credential,
        DateTime validUntil,
        CancellationToken cancellationToken
    )
    {
        if (!credential.Username.Contains(':')) { throw new InvalidOperationException($"'{nameof(credential.Username)}' must contain a colon."); }

        var loginIdentifier = Quote.Identifier(credential.Username);
        var validUntilLiteral = Quote.Literal(validUntil);

        using var maintenance = await DB.MaintenanceConnection(credential.Server, cancellationToken);
        using var batch = new NpgsqlBatch(maintenance)
        {
            BatchCommands = {
                new("CALL public.drop_expired_logins()"),
                new($"ALTER ROLE {loginIdentifier} VALID UNTIL {validUntilLiteral}"),
            }
        };

        return await batch.ExecuteNonQueryAsync(cancellationToken);
    }
}