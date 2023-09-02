using System.Security.Claims;
using System.Text.Json;

namespace NeonMS.Authentication;

public class CurrentUser
{
    private readonly ConnectionCredential? _credential;

    public CurrentUser(ClaimsPrincipal principal)
    {
        if (principal.FindFirstValue("cc") is string cc)
        {
            string[] creds = JsonSerializer.Deserialize<string[]>(cc)!;
            _credential = new ConnectionCredential(creds[0], creds[1], creds[2]);
        }
    }

    public ConnectionCredential Credential { get => _credential ?? throw new InvalidOperationException("Credentials not found."); }
}
