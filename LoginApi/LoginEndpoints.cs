using LoginMod;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LoginApi;

public class LoginEndpoints {
    public record LoginInput(string Username, string Password);

    public static async Task<IResult> Login(HttpContext context, LoginInput input, LoginServices service, CancellationToken cancel) {
        var login = await service.Find(input.Username, input.Password, cancel);

        if (login.LocalLoginId == LoginConstants.Unknown.LocalLoginId) {
            return Results.BadRequest("Invalid username or password.");
        }

        ClaimsIdentity identity = new(
            new Claim[]
            {
                new Claim("sub", login.LocalLoginId.ToString()),
                new Claim("name", login.Username),
            }
            .Concat(login.Roles.Select(role => new Claim("role", role))),
            "local",
            "name",
            "role"
        );
        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new() {
            //IsPersistent = ?,
        };

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
        return Results.Ok();
    }


    public static async Task<IResult> Logout(HttpContext context) {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }

    public record RegisterInput(string Username, string Password);

    public static async Task<IResult> Register(RegisterInput input, LoginServices service, CancellationToken cancel) {
        var errors = await service.Register(input.Username, input.Password, cancel);

        if (errors.Any()) {
            return Results.BadRequest(string.Join(" ", errors));
        }

        return Results.Ok();
    }

    public record LoginInfoResult(bool Auth, string Sub, string Name, string[] Roles) { }

    public static LoginInfoResult LoginInfo(HttpContext context) {
        var user = context?.User;
        return new LoginInfoResult(
            Auth: user?.Identity?.IsAuthenticated == true,
            Sub: user?.FindFirstValue("sub") ?? LoginConstants.Unknown.LocalLoginId.ToString(),
            Name: user?.FindFirstValue("name") ?? LoginConstants.Unknown.Username,
            Roles: user?.FindAll("role").Select(c => c.Value).ToArray() ?? Array.Empty<string>()
        );
    }
}
