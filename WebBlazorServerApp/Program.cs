using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
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

            // Identity
            var identityConnectionString = builder.Configuration.GetConnectionString("Identity") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(identityConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
            services.AddDatabaseDeveloperPageExceptionFilter();
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
