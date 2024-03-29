﻿using LinqToDB;
using LinqToDB.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using NeonMuon.Authentication;
using NeonMuon.Configuration;
using NeonMuon.Mvc;
using NeonMuon.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeonMuon.Tenancy;

public static class Starter
{
    public static async Task Main(string[] args, TenantInfo tenant, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions()
        {
            ApplicationName = null,
            Args = args,
            ContentRootPath = tenant.ContentRoot,
            EnvironmentName = tenant.EnvironmentName,
            WebRootPath = tenant.WebRoot,
        });
        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls(tenant.Urls);
        // if (tenant.Urls.Any(url => url.StartsWith("https:")))
        // {
        //     builder.WebHost.UseKestrelHttpsConfiguration();
        // }

        builder.Configuration.SetBasePath(tenant.ContentRoot);
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        builder.Logging
            .AddConfiguration(builder.Configuration.GetSection("Logging"))
            .AddConsole();

        // builder.Configuration.AddUserSecrets(typeof(Program).Assembly);

        // TODO? builder.Services.AddScoped(typeof(ScopedLazy<>));

        // OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }

        // Database
        {
            builder.BuildFromTypes(typeof(Starter).Assembly.ExportedTypes);

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

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped(x => x.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal());

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = builder.Environment.ApplicationName + "_" + tenant.Id + "_auth";
                    options.Events = new ApiAwareCookieAuthenticationEvents();
                });

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
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("up", () => Results.Ok());
        app.MapControllers();

        app.Map("/api/{*ignore}", () => Results.NotFound());
        app.MapFallbackToFile("/_content/NeonMuon/index.html");

        await app.RunAsync(cancellationToken);
    }
}