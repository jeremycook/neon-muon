using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var section = app.Configuration.GetSection("Hooks");
if (!section.Exists()) {
    throw new Exception("Missing Hooks section.");
}

var hooks = section.Get<Dictionary<string, string>>()!;

foreach (var hook in hooks) {
    app.Map("/" + hook.Key, () => {
        var output = new StringBuilder();
        var error = new StringBuilder();

        using var p = new Process();
        p.StartInfo.FileName = hook.Value;
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
            return Results.BadRequest(error.ToString());
        }
        else if (output.Length > 0) {
            return Results.Content(output.ToString());
        }
        else {
            return Results.Ok();
        }
    });
}

app.Run();
