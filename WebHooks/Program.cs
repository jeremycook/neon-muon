using FileMod;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration.Json;
using System.Diagnostics;
using System.Text;
using WebHooks;

var builder = WebApplication.CreateBuilder(args);

var appSettings = builder.AddDataDirectory(basePath => new AppSettings(basePath));
foreach (var source in builder.Configuration.Sources.OfType<JsonConfigurationSource>().ToArray()) {
    builder.Configuration.AddJsonFile(appSettings.GetFullPath(source.Path!), source.Optional, source.ReloadOnChange);
}

var jobQueue = new JobQueue();
builder.Services.AddSingleton(jobQueue);
builder.Services.AddHostedService<JobQueueHostedService>();

var hooks = builder.Configuration.GetSection("Hooks");
if (!hooks.Exists()) {
    throw new Exception("Missing Hooks section.");
}

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.All
});

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
            app.Logger.LogWarning("{HookPath} authentication from {RemoteIp} failed.", hookPath, context.Connection.RemoteIpAddress);
            return Results.StatusCode((int)authenticateResult);
        }

        app.Logger.LogInformation("{HookPath} authentication from {RemoteIp} succeeded.", hookPath, context.Connection.RemoteIpAddress);

        jobQueue.EnqueueTask((_, _) => {

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
                app.Logger.LogInformation("{HookPath} output: {StdOut}", hookPath, output.ToString());
            }

            if (error.Length > 0) {
                app.Logger.LogError("{HookPath} error: {StdError}", hookPath, error.ToString());
            }

            return Task.CompletedTask;
        });

        return Results.Ok();
    });
}

app.Run();
