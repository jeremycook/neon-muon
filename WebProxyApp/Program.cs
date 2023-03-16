var builder = WebApplication.CreateBuilder(args);

// Services

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// App

var app = builder.Build();

app.UseHttpsRedirection();
app.MapReverseProxy();

app.Run();
