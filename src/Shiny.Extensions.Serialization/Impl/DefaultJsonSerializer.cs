using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Shiny.Impl;


public class DefaultJsonSerializer : ISerializer
{
    /// <summary>
    /// The underlying <see cref="JsonSerializerOptions"/>. Production code should configure
    /// AOT-safe sources via <see cref="AddContext"/> (or <c>services.AddJsonContext(...)</c>).
    /// </summary>
    public JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
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


    // ---------- UTF-8 byte overloads (native STJ paths) ----------

    public byte[] SerializeToUtf8Bytes<T>(T value)
    {
        var typeInfo = this.GetRequiredTypeInfo(typeof(T));
        return JsonSerializer.SerializeToUtf8Bytes(value, typeInfo);
    }


    public T Deserialize<T>(ReadOnlySpan<byte> utf8Json)
    {
        var typeInfo = this.GetRequiredTypeInfo(typeof(T));
        return (T)JsonSerializer.Deserialize(utf8Json, typeInfo)!;
    }


    // ---------- Stream overloads (native STJ paths) ----------

    public Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
    {
        var typeInfo = this.GetRequiredTypeInfo(typeof(T));
        return JsonSerializer.SerializeAsync(stream, value, typeInfo, cancellationToken);
    }


    public async ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        var typeInfo = this.GetRequiredTypeInfo(typeof(T));
        var result = await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken).ConfigureAwait(false);
        return (T?)result;
    }


    public async IAsyncEnumerable<T?> DeserializeAsyncEnumerable<T>(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var typeInfo = (JsonTypeInfo<T>)this.GetRequiredTypeInfo(typeof(T));
        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable(stream, typeInfo, cancellationToken).ConfigureAwait(false))
            yield return item;
    }


    JsonTypeInfo GetRequiredTypeInfo(Type type)
    {
        JsonTypeInfo? typeInfo = null;
        try
        {
            typeInfo = this.Options.GetTypeInfo(type);
        }
        catch (InvalidOperationException) { }
        catch (NotSupportedException) { }

        if (typeInfo == null)
            throw new InvalidOperationException(
                $"No JsonTypeInfo registered for type '{type.FullName}'. " +
                $"Register a JsonSerializerContext containing this type via services.AddJsonContext(...), " +
                $"add [ShinyJsonInclude] to opt in collection support, or call Shiny.Json.AddContext/AddResolver."
            );

        return typeInfo;
    }
}
