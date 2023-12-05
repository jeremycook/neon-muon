using NeonMuon.Configuration;
using NeonMuon.DataAccess;
using System.Security.Claims;
using System.Text.Json;

namespace NeonMuon.Authentication;

[Scoped]
public class CurrentUser(ClaimsPrincipal principal)
{
    public IEnumerable<DataCredential> Credentials()
    {
        foreach (var claim in principal.Claims)
        {
            if (claim.Type.StartsWith("dc:"))
            {
                yield return Credential(claim.Type);
            }
        }
    }

    public DataCredential Credential(string dataServer)
    {
        // TODO: Cache results?
        if (principal.FindFirstValue("dc:" + dataServer) is string dc)
        {
            return JsonSerializer.Deserialize<DataCredential>(dc)!;
        }

        // TODO: Return anonymous credentials if available.
        return new()
        {
            Server = dataServer,
            Username = "unknown",
            Password = "unknown",
            Role = "",
        };
    }
}
