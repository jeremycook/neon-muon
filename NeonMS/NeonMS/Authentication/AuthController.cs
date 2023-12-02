using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeonMS.DataAccess;
using NeonMS.Mvc;
using NeonMS.Security;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace NeonMS.Authentication;

[ApiController]
[Route(MvcConstants.StandardApiRoute)]
public class AuthController(DB DB, DataServers DataServers) : ControllerBase
{
    public class AuthInput
    {
        [Required] public string DataServer { get; set; } = string.Empty;
        [Required] public string Username { get; set; } = string.Empty;
        [Required, MinLength(10)] public string Password { get; set; } = string.Empty;
    }

    public class AuthOutput
    {
        public required DateTime NotAfter { get; set; }
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<AuthOutput>>
    Login(
        AuthInput input,
        CancellationToken cancellationToken
    )
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!DataServers.TryGetValue(input.DataServer, out DataServer? dataServer))
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
        await AuthHelpers.CreateLogin(DB, temporaryCredential, notAfter, cancellationToken);

        var identity = new ClaimsIdentity(claims: [
            new("sub", temporaryCredential.Role),
            new("name", temporaryCredential.Role),
            new("dc", JsonSerializer.Serialize(temporaryCredential)),
        ], authenticationType: "pglogin", "sub", "role");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(principal, properties: new()
        {
            AllowRefresh = true,
            ExpiresUtc = notAfter,
        });
        return new AuthOutput()
        {
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

        DataCredential temporaryCredential = currentUser.Credential;
        if (!DataServers.TryGetValue(temporaryCredential.Server, out DataServer? dataServer))
        {
            ModelState.AddModelError("dataServer", "The Data Server is incorrect.");
            return ValidationProblem();
        }

        var tokenLifetimeHours = dataServer.TokenLifetimeHours;

        DateTime notAfter = DateTime.UtcNow.AddHours(tokenLifetimeHours);

        await AuthHelpers.RenewLogin(DB, temporaryCredential, notAfter, cancellationToken);

        var identity = new ClaimsIdentity(claims: [
            new("sub", temporaryCredential.Role),
            new("name", temporaryCredential.Role),
            new("dc", JsonSerializer.Serialize(temporaryCredential)),
        ], authenticationType: "pglogin", "sub", "role");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(principal, properties: new()
        {
            AllowRefresh = true,
            ExpiresUtc = notAfter,
        });
        return new AuthOutput()
        {
            NotAfter = notAfter,
        };
    }
}
