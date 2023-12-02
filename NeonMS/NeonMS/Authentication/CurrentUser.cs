using NeonMS.Configuration;
using NeonMS.DataAccess;
using System.Security.Claims;
using System.Text.Json;

namespace NeonMS.Authentication;

[Scoped]
public class CurrentUser
{
    public CurrentUser(ClaimsPrincipal principal)
    {
        if (principal.FindFirstValue("dc") is string dc)
        {
            Credential = JsonSerializer.Deserialize<DataCredential>(dc)!;
        }
        else
        {
            Credential = new()
            {
                Server = string.Empty,
                Username = string.Empty,
                Password = string.Empty,
                Role = string.Empty,
            };
        }
    }

    public DataCredential Credential { get; }
}
