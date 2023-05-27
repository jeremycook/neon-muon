//using DataCore;
//using DataCore.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFMod;

public static class StartupExtensions {
    //public static IServiceCollection AddDb<TDbService, TDbImpl>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    //    where TDbService : IDb<TDbService>
    //    where TDbImpl : class, TDbService {
    //    return services
    //        .AddPooledDbContextFactory<QueryDbContext<TDbService>>(optionsAction)
    //        .AddDbContextPool<QueryDbContext<TDbService>>(optionsAction)
    //        .AddScoped<IDbContext<TDbService>>(svc => svc.GetRequiredService<QueryDbContext<TDbService>>())
    //        .AddScoped<IDbContext>(svc => svc.GetRequiredService<QueryDbContext<TDbService>>())
    //        .AddScoped(typeof(TDbService), typeof(TDbImpl));
    //}
}
