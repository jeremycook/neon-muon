using DataCore;
using DataCore.EF;
using DataMod.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataMod;

public static class StartupExtensions
{
    public static IServiceCollection AddDb<TDbService, TDbImpl>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
        where TDbService : IDb<TDbService>
        where TDbImpl : class, TDbService
    {
        return services
            .AddPooledDbContextFactory<ComponentDbContext<TDbService>>(optionsAction)
            .AddDbContextPool<ComponentDbContext<TDbService>>(optionsAction)
            .AddScoped<IComponentDbContext<TDbService>>(svc => svc.GetRequiredService<ComponentDbContext<TDbService>>())
            .AddScoped<IComponentDbContext>(svc => svc.GetRequiredService<ComponentDbContext<TDbService>>())
            .AddScoped(typeof(TDbService), typeof(TDbImpl));
    }
}
