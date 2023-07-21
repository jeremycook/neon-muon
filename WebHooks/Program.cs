using FileMod;
using Microsoft.Extensions.Configuration.Json;
using System.Diagnostics;
using System.Text;
using WebHooks;

var builder = WebApplication.CreateBuilder(args);

var appSettings = builder.AddDataDirectory(basePath => new AppSettings(basePath));
foreach (var source in builder.Configuration.Sources.OfType<JsonConfigurationSource>()) {
    builder.Configuration.AddJsonFile(appSettings.GetFullPath(), source.Optional, source.ReloadOnChange);
}

var app = builder.Build();

var hooks = app.Configuration.GetSection("Hooks");
if (!hooks.Exists()) {
    throw new Exception("Missing Hooks section.");
}

foreach (var hook in hooks.GetChildren()) {
    var hookPath = hook.Key;
    var hookSettings = hook.Get<HookSettings>()!;
    app.Map("/" + hookPath, async (HttpContext context) => {

        IHookAuthenticator authenticator = hookSettings.Authenticator switch {
            HookAuthenticator.Unauthorized => new UnauthorizedAuthenticator(),
            HookAuthenticator.GitHub => new GitHubAuthenticator(),
            _ => throw new NotSupportedException($"The {hookSettings.Authenticator} hook authenticator is not supported."),
        };
        var authenticateResult = await authenticator.Authenticate(context, hook);
        if (authenticateResult != System.Net.HttpStatusCode.OK) {
            return Results.StatusCode((int)authenticateResult);
        }

        var output = new StringBuilder();
        var error = new StringBuilder();

        using var p = new Process();
        p.StartInfo.WorkingDirectory = hookSettings.WorkingDirectory;
        p.StartInfo.FileName = hookSettings.FileName;
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
            app.Logger.LogInformation("{HookName} std out: {StdOut}", hookPath, output.ToString());
        }

        if (error.Length > 0) {
            app.Logger.LogError("{HookName} std error: {StdError}", hookPath, error.ToString());
            return Results.BadRequest();
        }
        else {
            return Results.Ok();
        }
    });
}

app.Run();
