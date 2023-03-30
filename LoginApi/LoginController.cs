using LoginMod;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LoginApi;

public class LoginController : Controller
{
    public record LoginInput(string Username, string Password);

    public static async Task<IResult> Login(LoginInput input, LoginServices service, CancellationToken cancel)
    {
        var login = await service.Find(input.Username, input.Password, cancel);

        if (login.UserId == LoginConstants.Unknown.UserId)
        {
            return Results.BadRequest("Invalid username or password.");
        }

        ClaimsIdentity identity = new(
            new Claim[]
            {
                new Claim("sub", login.UserId.ToString()),
                new Claim("name", login.Username),
            },
            "local",
            "name",
            "role"
        );
        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            //IsPersistent = ?,
        };

        return Results.SignIn(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);
    }


    public record RegisterInput(string Username, string Password);

    public static async Task<IResult> Register(RegisterInput input, LoginServices service, CancellationToken cancel)
    {
        var login = await service.Register(input.Username, input.Password, cancel);

        if (login.UserId == LoginConstants.Unknown.UserId)
        {
            return Results.BadRequest("Invalid username or password.");
        }

        return Results.Ok();
    }
}
