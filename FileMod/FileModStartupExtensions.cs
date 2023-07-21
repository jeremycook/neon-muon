using FileMod;

namespace Microsoft.Extensions.DependencyInjection;

public static class FileModStartupExtensions {
    public static TRelativeData AddDataDirectory<TRelativeData>(this WebApplicationBuilder builder, Func<string, TRelativeData> factory) where TRelativeData : DataDirectory {
        var name = typeof(TRelativeData).Name;
        var sectionKey = name + "Dir";
        var defaultDir = name.ToLower();

        var dir = builder.Configuration.GetSection(sectionKey).Value;

        if (string.IsNullOrWhiteSpace(dir)) {
            dir = defaultDir;
        }

        dir = Path.GetFullPath(dir);

        builder.Configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[] {
            new(sectionKey, dir),
        });

        var configurationDir = builder.Configuration.GetValue<string>(sectionKey);
        if (dir != configurationDir) {
            throw new InvalidOperationException($"The {sectionKey} builder.Configuration is invalid.");
        }

        Console.WriteLine($"{sectionKey}: {dir}");

        if (builder.Environment.IsDevelopment()) {
            Directory.CreateDirectory(dir);
        }
        else if (!Directory.Exists(dir)) {
            throw new DirectoryNotFoundException($"Could not find a part of the path '{dir}'.");
        }

        TRelativeData relativeData = factory(dir);
        builder.Services.AddSingleton(relativeData);

        return relativeData;
    }
}
