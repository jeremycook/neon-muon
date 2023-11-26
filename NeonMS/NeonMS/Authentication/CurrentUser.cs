using System.Security.Claims;
using System.Text.Json;
using NeonMS.DataAccess;

namespace NeonMS.Authentication;

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
            // TODO: Get actual anonymous credential
            Credential = new()
            {
                Username = "Guest",
                Role = Guid.Empty.ToString(),
            };
        }
    }

    public DataCredential Credential { get; }
}
