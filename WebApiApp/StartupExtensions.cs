namespace Microsoft.Extensions.DependencyInjection;

public static class StartupExtensions {
    public static T? GetImplementationInstance<T>(this IServiceCollection services) where T : class {
        return services.LastOrDefault(o => o.ServiceType == typeof(T))?.ImplementationInstance as T;
    }
    public static T GetRequiredImplementationInstance<T>(this IServiceCollection services) where T : class {
        return
            services.GetImplementationInstance<T>()
            ?? throw new InvalidOperationException($"An implementation of {typeof(T)} was not found.");
    }
}
