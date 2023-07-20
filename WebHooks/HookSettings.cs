using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WebHooks;

public class HookSettings {
    public string? WorkingDirectory { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public HookAuthenticator Authenticator { get; set; }
    public JsonDocument AuthenticatorSettings { get; set; } = null!;
}

public interface IHookAuthenticator {
    Task<HttpStatusCode> Authenticate(HttpContext context, IConfigurationSection section);
}

public enum HookAuthenticator {
    Unauthorized = 0,
    GitHub = 1,
}

public class UnauthorizedAuthenticator : IHookAuthenticator {
    public Task<HttpStatusCode> Authenticate(HttpContext context, IConfigurationSection section) {
        return Task.FromResult(HttpStatusCode.Unauthorized);
    }
}

public record GitHubAuthenticatorSettings(string Secret) { }
public class GitHubAuthenticator : IHookAuthenticator {
    public async Task<HttpStatusCode> Authenticate(HttpContext context, IConfigurationSection section) {
        var secret = section.GetValue<string?>("Secret")
            ?? throw new ArgumentException("Missing GitHub Authenticator Secret.", nameof(section));

        var request = context.Request;

        // Verify that the signature and body match
        if (request.Headers.TryGetValue("X-Hub-Signature-256", out var providedSignature) &&
            !string.IsNullOrWhiteSpace(providedSignature)
        ) {
            var key = Encoding.ASCII.GetBytes(secret);
            var body = await ReadToEndAsync(request.Body);

            using var hmac = new HMACSHA256(key);
            var hashedBody = hmac.ComputeHash(body);

            var signature = "sha256=" + string.Concat(Array.ConvertAll(hashedBody, x => x.ToString("x2")));

            if (providedSignature != signature) {
                return HttpStatusCode.Unauthorized;
            }
        }
        else {
            return HttpStatusCode.Unauthorized;
        }

        return HttpStatusCode.OK;
    }

    private static async Task<byte[]> ReadToEndAsync(Stream stream) {
        if (stream is MemoryStream mem) {
            return mem.ToArray();
        }

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}
