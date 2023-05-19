using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebBlazorServerApp.Areas.Identity;
using WebBlazorServerApp.Areas.Identity.Data;

namespace WebBlazorServerApp;
public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        {
            IServiceCollection services = builder.Services;

            // EF Core
            services.AddDatabaseDeveloperPageExceptionFilter();

            // Identity (https://aka.ms/aspaccountconf)
            var identitySettings = builder.Configuration.GetRequiredSection(nameof(IdentitySettings)).Get<IdentitySettings>()!;
            var identityConnectionString = identitySettings.ConnectionString ?? throw new InvalidOperationException("Connection string 'IdentitySettings.ConnectionString' not found.");
            builder.Services.AddSingleton(identitySettings);
            builder.Services.AddTransient<IEmailSender, IdentityEmailSender>();
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(identityConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
            services
                .AddDefaultIdentity<IdentityUser>(options => {
                    options.SignIn.RequireConfirmedAccount = true;
                    // NIST
                    options.Password.RequiredLength = 12;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                })
                .AddEntityFrameworkStores<IdentityDbContext>();

            // ASP.NET
            services.AddRazorPages()
                .AddJsonOptions(configure => {
                    // Ensure actions and action filters have access to formatter exception messages
                    configure.AllowInputFormatterExceptionMessages = true;

                    // Be more forgiving about JSON input
                    configure.JsonSerializerOptions.AllowTrailingCommas = true;
                    configure.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                    configure.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    // TODO: Write a JsonEnumConverter that can deserialize from string or integer,
                    // and serialize to int. Since JsonStringEnumConverter serializes to string
                    // that could be in conflict with using enums in TypeScript.
                    //configure.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            services.AddServerSideBlazor();
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

            // Application
            // Ex: services.AddSingleton<WeatherForecastService>();
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseMigrationsEndPoint();
        }
        else {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllers();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}
