using Microsoft.AspNetCore.Mvc;
using NeonMS.DataAccess;
using NeonMS.Security;
using NeonMS.Utils;
using System.ComponentModel.DataAnnotations;

namespace NeonMS.Authentication;

[ApiController]
public class AuthController : ControllerBase
{
    public class AuthenticateInput
    {
        [Required] public string? Connection { get; set; }
        [Required] public string? Username { get; set; }
        [Required, MinLength(20)] public string? Password { get; set; }
    }

    [HttpPost("[controller]")]
    public async Task<IActionResult> Default(
        Keys keys,
        ScopedLazy<CurrentCredentials> currentCredentials,
        AuthenticateInput input
    )
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var credential = new KeyValuePair<string, ConnectionCredential>(input.Connection!, new(input.Username!, input.Password!));

        using var dc = await ConnectionFactory.TryDataConnection(credential);

        if (dc is not null)
        {
            var cns = new Dictionary<string, ConnectionCredential>(currentCredentials.Value, StringComparer.OrdinalIgnoreCase);
            cns.Remove(credential.Key);
            cns.Add(credential.Key, credential.Value);

            string token = TokenService.GetToken(keys, DateTime.UtcNow.AddDays(30), new Dictionary<string, object>()
            {
                { "cns", cns },
            });
            return Ok(token);
        }
        else
        {
            return Unauthorized();
        }
    }

    [HttpGet("[controller]/[action]")]
    public IActionResult Me()
    {
        return Ok(new
        {
            auth = User.Identity!.IsAuthenticated,
        });
    }
}
