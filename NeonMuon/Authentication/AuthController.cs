using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeonMuon.DataAccess;
using NeonMuon.Mvc;
using NeonMuon.Security;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace NeonMuon.Authentication;

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

        var dcClaim = new Claim("dc:" + temporaryCredential.Server, JsonSerializer.Serialize(temporaryCredential));

        // Include other, existing dc: claims
        var dcClaims = User
            .Claims
            .Where(x => x.Type.StartsWith("dc:") && x.Type != dcClaim.Type)
            .Append(dcClaim)
            .ToArray();

        var identity = new ClaimsIdentity(claims: [
            User.FindFirst("sub") ?? new("sub", temporaryCredential.Role),
            User.FindFirst("name") ?? new("name", temporaryCredential.Role),
            ..dcClaims,
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

        DateTime maxNotAfter = DateTime.UtcNow;
        var temporaryCredentials = new List<DataCredential>();
        foreach (var credential in currentUser.Credentials())
        {
            if (!DataServers.TryGetValue(credential.Server, out DataServer? dataServer))
            {
                ModelState.AddModelError("dataServer", "The Data Server is incorrect.");
                continue;
            }

            var tokenLifetimeHours = dataServer.TokenLifetimeHours;
            {
                var notAfter = DateTime.UtcNow.AddHours(tokenLifetimeHours);
                if (notAfter > maxNotAfter)
                    maxNotAfter = notAfter;
            }

            await AuthHelpers.RenewLogin(DB, credential, maxNotAfter, cancellationToken);

            temporaryCredentials.Add(credential);
        }

        var identity = new ClaimsIdentity(claims: [
            User.FindFirst("sub") ?? throw new InvalidOperationException("Missing sub claim."),
            User.FindFirst("name") ?? throw new InvalidOperationException("Missing name claim."),
            ..temporaryCredentials.Select(x =>
                new Claim("dc:" + x.Server, JsonSerializer.Serialize(x))
            ),
        ], authenticationType: "renew", "sub", "role");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(principal, properties: new()
        {
            AllowRefresh = true,
            ExpiresUtc = maxNotAfter,
        });
        return new AuthOutput()
        {
            NotAfter = maxNotAfter,
        };
    }
}
