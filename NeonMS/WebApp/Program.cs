using LinqToDB;
using LinqToDB.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using NeonMS;
using NeonMS.Authentication;
using NeonMS.DataAccess;
using NeonMS.Mvc;
using NeonMS.Security;
using NeonMS.Utils;
using Npgsql;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

Log.Factory = LoggerFactory.Create(options => options
    .AddConfiguration(builder.Configuration.GetSection("Logging"))
    .AddConsole());

builder.Services.AddScoped(typeof(ScopedLazy<>));

// Database
{
    DB.Connections = builder.Configuration
        .GetSection("Connections")
        .Get<Dictionary<string, NpgsqlConnectionStringBuilder>>()
        ?? throw new InvalidOperationException();
    Log.Info<DataConnection>("Configuration Connections: {Connections}", DB.Connections.Select(x => x.Key + ": " + Regex.Replace(x.Value.ConnectionString, "(Pass[^=]*|Pwd[^=]*)[^;]+", "$1=***", RegexOptions.IgnoreCase)));
    DataConnection.DefaultSettings = new AppLinqToDBSettings();

    if (builder.Environment.IsDevelopment())
    {
        DataConnection.DefaultOnTraceConnection = (traceInfo) =>
        {
            Log.Info<DataConnection>("{TraceInfoStep} {SqlText}", traceInfo.TraceInfoStep, traceInfo.SqlText);
        };
        DataConnection.TurnTraceSwitchOn();
    }
}

// Authentication
{
    if (builder.Environment.IsDevelopment())
    {
        Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
    }

    Keys keys;
    {
        var signingKeys = builder.Configuration.GetSection("Keys:SigningKeys").Get<byte[][]>()!;
        var decryptionKeys = builder.Configuration.GetSection("Keys:DecryptionKeys").Get<byte[][]>()!;
        keys = new(signingKeys, decryptionKeys);
        builder.Services.AddSingleton(keys);
    }

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped(x => x.GetRequiredService<IHttpContextAccessor>().HttpContext!.User);
    builder.Services.AddScoped<CurrentUser>();

    builder.Services.AddAuthentication(auth =>
    {
        auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
        .AddJwtBearer(jwt =>
        {
            jwt.SaveToken = true;
            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                //NameClaimType = "sub",
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys.SigningKeys,
                TokenDecryptionKeys = keys.DecryptionKeys,
                ValidateLifetime = true,
                //LifetimeValidator = LifetimeValidator
            };
            jwt.Events = new()
            {
                OnTokenValidated = OnTokenValidated,
            };
        });
}

Task OnTokenValidated(TokenValidatedContext context)
{
    return Task.CompletedTask;
}

builder.Services.AddControllers(options =>
{
    // Enforce the default authorization policy on controllers
    options.Filters.Add(new AuthorizeFilter());

    // Exception filters we control
    //options.Filters.Add<CustomExceptionFilter>();

    // Slugify paths
    options.Conventions.Add(new RouteTokenTransformerConvention(new CustomOutboundParameterTransformer(TextTransformers.Dashify)));
})
    // ASP.NET MVC API
    .AddJsonOptions(options => ConfigureJsonSerializerOptions(options.JsonSerializerOptions));

// Minimal API
builder.Services.Configure<JsonOptions>(options => ConfigureJsonSerializerOptions(options.SerializerOptions));

static void ConfigureJsonSerializerOptions(JsonSerializerOptions json)
{
    json.Converters.Add(new JsonStringEnumConverter());
    json.AllowTrailingCommas = true;
    json.PropertyNameCaseInsensitive = true;
    json.ReadCommentHandling = JsonCommentHandling.Skip;
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("up", () => Results.Ok());
app.MapControllers();

app.Run();