using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataMod;

public static class StartupExtensions
{
    public static IServiceCollection AddDb<TDb>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
        where TDb : Db<TDb>
    {
        return services
            .AddPooledDbContextFactory<ComponentDbContext<TDb>>(optionsAction)
            .AddDbContextPool<ComponentDbContext<TDb>>(optionsAction)
            .AddScoped<TDb>();
    }
}
