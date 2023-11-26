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
    private const int expireDays = 30;

    public class AuthInput
    {
        [Required] public string? Username { get; set; }
        [Required, MinLength(10)] public string? Password { get; set; }
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<string>>
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

        var credential = new DataCredential()
        {
            Username = input.Username!,
            Password = input.Password!,
        };

        bool validCredentials = await DB.IsValid(credential, DB.DirectoryDatabase, cancellationToken);
        if (!validCredentials)
        {
            ModelState.AddModelError("", "The Password or Username is incorrect.");
            return ValidationProblem(ModelState);
        }

        DateTime validUntil = DateTime.UtcNow.AddDays(expireDays);
        var newCredential = new DataCredential()
        {
            Username = $"{credential.Username}:{DateTime.UtcNow:yyyyMMddHHmmss}",
            Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            Role = credential.Username,
        };

        {
            using var maintenance = await DB.MaintenanceConnection(cancellationToken);
            await CreateLogin(maintenance, newCredential, validUntil, cancellationToken);
        }

        string token = TokenService.GetToken(keys, validUntil, new Dictionary<string, object>()
        {
            { "dc", JsonSerializer.Serialize(newCredential) },
        });

        return Ok(token);
    }

    private static async Task<int>
    CreateLogin(
        NpgsqlConnection connection,
        DataCredential credential,
        DateTime validUntil,
        CancellationToken cancellationToken
    )
    {
        if (credential.Role == null)
        {
            throw new NullReferenceException("The value of credential.Role is null.");
        }

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
            }
        };

        return await batch.ExecuteNonQueryAsync(cancellationToken);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult
    Current()
    {
        return Ok(new
        {
            Auth = User.Identity?.IsAuthenticated == true,
        });
    }

    [AllowAnonymous]
    [HttpPut]
    public IActionResult
    Current(CurrentUser currentUser)
    {
        // TODO: Make this right
        return Ok(new
        {
            Auth = User.Identity?.IsAuthenticated == true,
            Sub = currentUser.Credential.Username,
            Name = currentUser.Credential.Role,
            Elevated = false,
            Roles = Array.Empty<string>(),
        });
    }
}
