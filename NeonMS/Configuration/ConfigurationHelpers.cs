using System.Reflection;

namespace NeonMS.Configuration;

public static class ConfigurationHelpers
{
    /// <summary>
    /// Modifies the <paramref name="builder"/> by adding services from <paramref name="candidateTypes"/>, etc.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="candidateTypes"></param>
    /// <exception cref="NullReferenceException"></exception>
    public static void BuildFromTypes(this IHostApplicationBuilder builder, IEnumerable<Type> candidateTypes)
    {
        foreach (var candidateType in candidateTypes)
        {
            if (candidateType.GetCustomAttribute<SettingsAttribute>() is not null)
            {
                var section = builder.Configuration.GetRequiredSection(candidateType.Name);
                var implementationInstance = section.Get(candidateType) ?? throw new NullReferenceException();

                builder.Services.AddSingleton(candidateType, implementationInstance);
            }
            else if (candidateType.GetCustomAttribute<SingletonAttribute>() is not null)
            {
                builder.Services.AddSingleton(candidateType);
            }
            else if (candidateType.GetCustomAttribute<ScopedAttribute>() is not null)
            {
                builder.Services.AddScoped(candidateType);
            }
        }
    }
}