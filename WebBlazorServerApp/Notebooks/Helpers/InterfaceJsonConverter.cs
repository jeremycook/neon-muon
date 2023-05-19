using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebBlazorServerApp.Notebooks.Helpers;

public static class InterfaceJsonConverter {
    public static InterfaceJsonConverter<TInterface> Create<TInterface>(params Assembly[] scanAssemblies) {
        var implementationTypes = scanAssemblies
            .DefaultIfEmpty(typeof(TInterface).Assembly)
            .SelectMany(a => a.ExportedTypes)
            .Where(t => t.IsAssignableTo(typeof(TInterface)))
            .ToImmutableDictionary(t => t.Name);

        return new InterfaceJsonConverter<TInterface>(implementationTypes);
    }
}

public class InterfaceJsonConverter<TInterface> : JsonConverter<TInterface> {
    private readonly ImmutableDictionary<string, Type> implementationTypes;

    public InterfaceJsonConverter(ImmutableDictionary<string, Type> implementationTypes) {
        this.implementationTypes = implementationTypes;
    }

    public override bool CanConvert(Type type) {
        return type == typeof(TInterface);
    }

    public override TInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (JsonDocument.TryParseValue(ref reader, out var doc)) {
            if (doc.RootElement.TryGetProperty("Type", out JsonElement typeElement)) {
                var typeName = typeElement.GetString();
                var type = implementationTypes[typeName];

                object? value = doc.RootElement.Deserialize(type, options);
                return (TInterface)value!;
            }

            throw new JsonException("Failed to extract \"Type\" property, is it missing?");
        }

        throw new JsonException("Failed to parse JsonDocument");
    }

    public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options) {
        JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(TInterface), options);
    }
}
