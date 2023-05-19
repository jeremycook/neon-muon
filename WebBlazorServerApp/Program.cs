using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using WebBlazorServerApp.Areas.Identity;
using WebBlazorServerApp.Areas.Identity.Data;
using WebBlazorServerApp.Data;

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
                    options.Password.RequiredLength = 12;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                })
                .AddEntityFrameworkStores<IdentityDbContext>();

            // ASP.NET
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

            // Application
            services.AddSingleton<WeatherForecastService>();
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
