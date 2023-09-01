using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeonMS.DataAccess;
using NeonMS.Security;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NeonMS.Authentication;

[ApiController]
public class AuthController : ControllerBase
{
    private const int expireDays = 30;

    public class AuthInput
    {
        [Required] public string? Connection { get; set; }
        [Required] public string? Username { get; set; }
        [Required, MinLength(20)] public string? Password { get; set; }
    }

    [AllowAnonymous]
    [HttpPost("[controller]")]
    public async Task<IActionResult> Default(
        Keys keys,
        AuthInput input
    )
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var credential = new ConnectionCredential(input.Connection!, input.Username!, input.Password!);

        using var dc = await ConnectionFactory.TryDataConnection(credential);

        if (dc is not null)
        {
            string token = TokenService.GetToken(keys, DateTime.UtcNow.AddDays(expireDays), new Dictionary<string, object>()
            {
                { "cc", JsonSerializer.Serialize(new string[] { credential.Connection, credential.Username, credential.Password }) },
            });
            return Ok(token);
        }
        else
        {
            return Unauthorized();
        }
    }

    [AllowAnonymous]
    [HttpGet("[controller]/[action]")]
    public IActionResult Current()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
        });
    }

    [AllowAnonymous]
    [HttpPut("[controller]/[action]")]
    public IActionResult Current(CurrentUser currentUser)
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            currentUser.Credential?.Connection,
            currentUser.Credential?.Username
        });
    }
}
