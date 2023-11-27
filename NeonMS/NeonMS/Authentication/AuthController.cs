using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeonMS.DataAccess;
using NeonMS.Mvc;
using NeonMS.Security;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;

namespace NeonMS.Authentication;

[ApiController]
[Route(MvcConstants.StandardApiRoute)]
public class AuthController : ControllerBase
{
    public class AuthInput
    {
        [Required] public string DataServer { get; set; } = string.Empty;
        [Required] public string Username { get; set; } = string.Empty;
        [Required, MinLength(10)] public string Password { get; set; } = string.Empty;
    }

    public class AuthOutput
    {
        public required string Token { get; set; }
        public required DateTime NotAfter { get; set; }
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<AuthOutput>>
    Login(
        Keys keys,
        AuthInput input,
        CancellationToken cancellationToken
    )
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!DB.Servers.TryGetValue(input.DataServer, out DataServer? dataServer))
        {
            ModelState.AddModelError("dataServer", "The Data Server is incorrect.");
            return ValidationProblem();
        }

        var credential = new DataCredential()
        {
            Server = input.DataServer,
            Username = input.Username!,
            Password = input.Password!,
            Role = string.Empty,
        };

        bool validCredentials = await DB.IsValid(credential, cancellationToken);
        if (!validCredentials)
        {
            ModelState.AddModelError("$", "The Username or Password is incorrect.");
            return ValidationProblem();
        }

        var tokenLifetimeHours = dataServer.TokenLifetimeHours;

        DateTime notAfter = DateTime.UtcNow.AddHours(tokenLifetimeHours);
        var temporaryCredential = new DataCredential()
        {
            Server = credential.Server,
            Username = $"{credential.Username}:{notAfter:yyyyMMddHHmmss}",
            Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            Role = credential.Username,
        };

        {
            using var maintenance = await DB.MaintenanceConnection(input.DataServer!, cancellationToken);
            await CreateLogin(maintenance, temporaryCredential, notAfter, cancellationToken);
        }

        string token = TokenService.CreateToken(keys, notAfter, new Dictionary<string, object>()
        {
            { "dc", JsonSerializer.Serialize(temporaryCredential) },
        });

        return new AuthOutput()
        {
            Token = token,
            NotAfter = notAfter,
        };
    }

    private static async Task<int>
    CreateLogin(
        NpgsqlConnection connection,
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

        using var batch = new NpgsqlBatch(connection)
        {
            BatchCommands = {
                new($"""
                CREATE ROLE {newLoginIdentifier} WITH
                    LOGIN
                    NOSUPERUSER
                    NOCREATEDB
                    NOCREATEROLE
                    INHERIT
                    NOREPLICATION
                    CONNECTION LIMIT -1
                    VALID UNTIL {validUntilLiteral}
                    ENCRYPTED PASSWORD {newPasswordLiteral}
                """),
                new($"GRANT {grantedRoleIdentifier} TO {newLoginIdentifier}"),
                new($"ALTER ROLE {newLoginIdentifier} SET role TO {grantedRoleIdentifier}"),
                // TODO: Call this on a timer from a background service
                new("CALL public.drop_expired_logins()"),
            }
        };

        return await batch.ExecuteNonQueryAsync(cancellationToken);
    }
}
