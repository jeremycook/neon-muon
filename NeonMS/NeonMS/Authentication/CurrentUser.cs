using System.Security.Claims;
using System.Text.Json;

namespace NeonMS.Authentication;

public class CurrentUser
{
    public CurrentUser(ClaimsPrincipal principal)
    {
        if (principal.FindFirstValue("cc") is string cc)
        {
            string[] creds = JsonSerializer.Deserialize<string[]>(cc)!;
            Credential = new ConnectionCredential(creds[0], creds[1], creds[2]);
        }
    }

    public ConnectionCredential? Credential { get; }
}
