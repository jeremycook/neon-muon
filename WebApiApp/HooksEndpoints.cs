using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace WebApiApp;

public static class HooksEndpoints {
    public record GitHubOptions(string Secret, string ApplicationName, string WorkingDirectory = "");
    public static async Task<IResult> GitHub(HttpRequest request, GitHubOptions gitHubOptions) {

        // Verify that the signature and body match
        if (request.Headers.TryGetValue("X-Hub-Signature-256", out var providedSignature) &&
            !string.IsNullOrWhiteSpace(providedSignature)
        ) {
            var key = Encoding.ASCII.GetBytes(gitHubOptions.Secret);
            var body = await request.Body.ReadToEndAsync();

            using var hmac = new HMACSHA256(key);
            var hashedBody = hmac.ComputeHash(body);

            var signature = "sha256=" + string.Concat(Array.ConvertAll(hashedBody, x => x.ToString("x2")));

            if (providedSignature != signature) {
                return Results.Unauthorized();
            }
        }
        else {
            return Results.Unauthorized();
        }

        var output = new StringBuilder();
        var error = new StringBuilder();

        using var p = new Process();
        p.StartInfo.WorkingDirectory = gitHubOptions.WorkingDirectory;
        p.StartInfo.FileName = gitHubOptions.ApplicationName;
        //p.StartInfo.Arguments = @"TODO";
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardInput = false;
        p.OutputDataReceived += (a, b) => output.Append(b.Data);
        p.ErrorDataReceived += (a, b) => error.Append(b.Data);
        p.Start();
        p.BeginErrorReadLine();
        p.BeginOutputReadLine();
        p.WaitForExit();

        if (error.Length > 0) {
            // TODO: Log error.ToString()
            return Results.StatusCode(500);
        }
        else if (output.Length > 0) {
            return Results.Content(output.ToString());
        }
        else {
            return Results.Ok();
        }
    }

    public static async Task<byte[]> ReadToEndAsync(this Stream stream) {
        if (stream is MemoryStream mem) {
            return mem.ToArray();
        }

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}
