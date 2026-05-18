using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Shiny.Extensions.Stores.Infrastructure;


public class DefaultSerializer : ISerializer
{
    /// <summary>
    /// The underlying <see cref="JsonSerializerOptions"/>. Production code should configure
    /// AOT-safe sources via <see cref="AddContext"/> (or <c>services.AddJsonContext(...)</c>).
    /// </summary>
    public JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };


    /// <summary>
    /// Registers a <see cref="JsonSerializerContext"/> so its types can be
    /// serialized/deserialized in an AOT-compatible way.
    /// </summary>
    public void AddContext(JsonSerializerContext context)
        => this.Options.TypeInfoResolverChain.Add(context);


    public T Deserialize<T>(string value)
    {
        var typeInfo = this.GetRequiredTypeInfo(typeof(T));
        return (T)JsonSerializer.Deserialize(value, typeInfo)!;
    }


    public string Serialize<T>(T value)
    {
        var typeInfo = this.GetRequiredTypeInfo(typeof(T));
        return JsonSerializer.Serialize(value, typeInfo);
    }


    public object? Deserialize(Type type, string value)
    {
        var typeInfo = this.GetRequiredTypeInfo(type);
        return JsonSerializer.Deserialize(value, typeInfo);
    }


    public string Serialize(object value, Type? declaredType = null)
    {
        var typeInfo = this.GetRequiredTypeInfo(declaredType ?? value.GetType());
        return JsonSerializer.Serialize(value, typeInfo);
    }


    JsonTypeInfo GetRequiredTypeInfo(Type type)
    {
        JsonTypeInfo? typeInfo = null;
        try
        {
            typeInfo = this.Options.GetTypeInfo(type);
        }
        catch (InvalidOperationException) { }

        if (typeInfo == null)
            throw new InvalidOperationException(
                $"No JsonTypeInfo registered for type '{type.FullName}'. " +
                $"Register a JsonSerializerContext containing this type via services.AddJsonContext(...)."
            );

        return typeInfo;
    }
}
