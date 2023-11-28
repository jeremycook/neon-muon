using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeonMS.DataAccess;
using NeonMS.Mvc;
using NeonMS.Security;
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
            Username = $"{credential.Username}:{notAfter:yyyyMMddHHmmss}Z",
            Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            Role = credential.Username,
        };

        await AuthHelpers.CreateLogin(temporaryCredential, notAfter, cancellationToken);

        string token = TokenHelpers.CreateToken(keys, notAfter, new Dictionary<string, object>()
        {
            { "dc", JsonSerializer.Serialize(temporaryCredential) },
        });

        return new AuthOutput()
        {
            Token = token,
            NotAfter = notAfter,
        };
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuthOutput>>
    Renew(
        Keys keys,
        CurrentUser currentUser,
        CancellationToken cancellationToken
    )
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        DataCredential credential = currentUser.Credential;
        if (!DB.Servers.TryGetValue(credential.Server, out DataServer? dataServer))
        {
            ModelState.AddModelError("dataServer", "The Data Server is incorrect.");
            return ValidationProblem();
        }

        var tokenLifetimeHours = dataServer.TokenLifetimeHours;

        DateTime notAfter = DateTime.UtcNow.AddHours(tokenLifetimeHours);

        await AuthHelpers.RenewLogin(credential, notAfter, cancellationToken);

        string token = TokenHelpers.CreateToken(keys, notAfter, new Dictionary<string, object>()
        {
            { "dc", JsonSerializer.Serialize(credential) },
        });

        return new AuthOutput()
        {
            Token = token,
            NotAfter = notAfter,
        };
    }
}
