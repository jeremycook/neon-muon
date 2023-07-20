using System.Diagnostics;
using System.Text;
using WebHooks;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var sections = app.Configuration.GetSection("Hooks");
if (!sections.Exists()) {
    throw new Exception("Missing Hooks section.");
}

foreach (var hookSection in sections.GetChildren()) {
    var hook = hookSection.Get<HookSettings>()!;
    app.Map("/" + hookSection.Key, async (HttpContext context) => {

        IHookAuthenticator authenticator = hook.Authenticator switch {
            HookAuthenticator.Unauthorized => new UnauthorizedAuthenticator(),
            HookAuthenticator.GitHub => new GitHubAuthenticator(),
            _ => throw new NotSupportedException($"The {hook.Authenticator} hook authenticator is not supported."),
        };
        var authenticateResult = await authenticator.Authenticate(context, hookSection);
        if (authenticateResult != System.Net.HttpStatusCode.OK) {
            return Results.StatusCode((int)authenticateResult);
        }

        var output = new StringBuilder();
        var error = new StringBuilder();

        using var p = new Process();
        p.StartInfo.WorkingDirectory = hook.WorkingDirectory;
        p.StartInfo.FileName = hook.FileName;
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

        if (output.Length > 0) {
            app.Logger.LogInformation("{HookName} std out: {StdOut}", hookSection.Key, output.ToString());
        }

        if (error.Length > 0) {
            app.Logger.LogError("{HookName} std error: {StdError}", hookSection.Key, error.ToString());
            return Results.BadRequest();
        }
        else {
            return Results.Ok();
        }
    });
}

app.Run();
