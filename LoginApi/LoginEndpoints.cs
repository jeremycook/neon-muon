using LoginMod;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LoginApi;

public class LoginEndpoints {
    public record ChangePasswordInput(string Username, string Password, string NewPassword);

    public static async Task<IResult> ChangePassword(ChangePasswordInput input, LoginServices service, CancellationToken cancel) {
        var errors = await service.ChangePassword(input.Username, input.Password, input.NewPassword, cancel);

        if (errors.Any()) {
            return Results.BadRequest(string.Join(" ", errors));
        }

        return Results.Ok();
    }

    public record LoginInput(string Username, string Password, bool RequestElevated = false);

    public static async Task<IResult> Login(LoginInput input, LoginServices service, CancellationToken cancel) {
        var login = await service.Find(input.Username, input.Password, cancel);

        if (login.LocalLoginId == LoginConstants.Unknown.LocalLoginId) {
            return Results.BadRequest("Invalid username or password.");
        }

        if (input.RequestElevated && !login.Roles.Contains("Admin")) {
            return Results.BadRequest("Only Admin can request the elevated permission.");
        }

        List<Claim> claims = new() {
            new Claim("sub", login.LocalLoginId.ToString()),
            new Claim("name", login.Username),
        };
        claims.AddRange(login.Roles.Select(role => new Claim("role", role)));
        if (input.RequestElevated && login.Roles.Contains("Admin")) {
            claims.Add(new Claim("Elevated", ""));
        }

        ClaimsIdentity identity = new(
            claims: claims,
            authenticationType: "local",
            nameType: "name",
            roleType: "role"
        );
        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new() {
            //IsPersistent = TODO?,
        };

        return Results.SignIn(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);
    }


    public static IResult Logout() {
        return Results.SignOut();
    }

    public record RegisterInput(string Username, string Password);

    public static async Task<IResult> Register(RegisterInput input, LoginServices service, CancellationToken cancel) {
        var errors = await service.Register(input.Username, input.Password, cancel);

        if (errors.Any()) {
            return Results.BadRequest(string.Join(" ", errors));
        }

        return Results.Ok();
    }

    public record LoginInfoResult(bool Auth, string Sub, string Name, bool Elevated, string[] Roles) { }

    public static LoginInfoResult LoginInfo(HttpContext context) {
        var user = context?.User;
        return new LoginInfoResult(
            Auth: user?.Identity?.IsAuthenticated == true,
            Sub: user?.FindFirstValue("sub") ?? LoginConstants.Unknown.LocalLoginId.ToString(),
            Name: user?.FindFirstValue("name") ?? LoginConstants.Unknown.Username,
            Elevated: user?.FindFirst("Elevated") != null,
            Roles: user?.FindAll("role").Select(c => c.Value).ToArray() ?? Array.Empty<string>()
        );
    }
}
