using System.Security.Claims;
using System.Text.Json;
using NeonMS.DataAccess;

namespace NeonMS.Authentication;

public class CurrentUser
{
    public CurrentUser(ClaimsPrincipal principal)
    {
        string dc = principal.FindFirstValue("dc")!;
        Credential = JsonSerializer.Deserialize<DataCredential>(dc)!;
    }

    public DataCredential Credential { get; }
}
