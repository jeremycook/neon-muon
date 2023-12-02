using LinqToDB;
using LinqToDB.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.IdentityModel.Tokens;
using NeonMS.Authentication;
using NeonMS.DataAccess;
using NeonMS.Mvc;
using NeonMS.Security;
using NeonMS.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeonMS.Tenancy;

public static class Starter
{
    public static async Task Main(string[] args, TenantInfo tenant)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
        {
            ApplicationName = tenant.ApplicationName,
            Args = args,
            ContentRootPath = tenant.ContentRoot,
            EnvironmentName = tenant.EnvironmentName,
            WebRootPath = tenant.WebRoot,
        });
        builder.WebHost.UseUrls(tenant.Urls);

        // builder.Configuration.AddUserSecrets(typeof(Program).Assembly);

        builder.Services.AddScoped(typeof(ScopedLazy<>));

        // Database
        {
            foreach (var (key, value) in builder.Configuration
                .GetRequiredSection("DataServers")
                .Get<Dictionary<string, DataServer>>()
                ?? throw new InvalidOperationException())
            {
                DB.Servers.Add(key, value);
            }
            Log.Info<DataConnection>("DataServers: {DataServers}", DB.Servers.Select(x => x.Key + ": " + x.Value.ToString()));

            foreach (var (key, value) in builder.Configuration
                .GetRequiredSection("MaintenanceCredentials")
                .Get<Dictionary<string, MaintenanceCredential>>()
                ?? throw new InvalidOperationException())
            {
                DB.MaintenanceCredentials.Add(key, value);
            }

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

            static Task OnTokenValidated(TokenValidatedContext context)
            {
                return Task.CompletedTask;
            }

            builder.Services.AddAuthorization();
        }

        // API
        {
            builder.Services.AddControllers(options =>
            {
                // Apply the default authorization policy to all controllers
                options.Filters.Add(new AuthorizeFilter());

                // Exception filters we control
                options.Filters.Add<CustomExceptionFilter>();

                // Slugify paths
                options.Conventions.Add(new RouteTokenTransformerConvention(new CustomOutboundParameterTransformer(TextTransformers.Dashify)));

                // JSON encode Controller.Ok(string) results
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
            })
                // ASP.NET MVC API
                .AddJsonOptions(options => ConfigureJsonSerializerOptions(options.JsonSerializerOptions));

            // Minimal API (app.MapGet, app.MapPost, etc.)
            builder.Services.Configure<JsonOptions>(options => ConfigureJsonSerializerOptions(options.SerializerOptions));

            static void ConfigureJsonSerializerOptions(JsonSerializerOptions json)
            {
                json.Converters.Add(new JsonStringEnumConverter());
                json.AllowTrailingCommas = true;
                json.PropertyNameCaseInsensitive = true;
                json.ReadCommentHandling = JsonCommentHandling.Skip;
            }
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("up", () => Results.Ok());
        app.MapControllers();

        await app.RunAsync();
    }
}