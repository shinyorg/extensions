using System.Collections.Concurrent;
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


    public DefaultJsonSerializer()
    {
        // The built-in context stays last on the options' own chain. Registered contexts still win, and a
        // nullable primitive works: Guid?'s generated converter looks up Guid through these options, so
        // resolving Guid? from the built-in context directly is not enough on its own.
        this.Options.TypeInfoResolverChain.Add(BuiltInJsonContext.Default);
    }


    /// <summary>
    /// Registers a <see cref="JsonSerializerContext"/> so its types can be
    /// serialized/deserialized in an AOT-compatible way.
    /// </summary>
    public void AddContext(JsonSerializerContext context)
        => this.AddResolver(context);


    /// <summary>Adds a resolver ahead of the built-in primitives, so it takes precedence over them.</summary>
    internal void AddResolver(IJsonTypeInfoResolver resolver)
    {
        var chain = this.Options.TypeInfoResolverChain;
        var builtIn = chain.IndexOf(BuiltInJsonContext.Default);
        chain.Insert(builtIn < 0 ? chain.Count : builtIn, resolver);
    }


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


    readonly ConcurrentDictionary<Type, JsonTypeInfo> builtIns = new();

    JsonTypeInfo GetRequiredTypeInfo(Type type)
    {
        if (this.builtIns.TryGetValue(type, out var builtIn))
            return builtIn;

        JsonTypeInfo? typeInfo = null;
        try
        {
            typeInfo = this.Options.GetTypeInfo(type);
        }
        catch (InvalidOperationException) { }
        catch (NotSupportedException) { }

        // The built-in context is already on the chain; this only catches a configurator that replaced
        // Options.TypeInfoResolver outright, so primitives still work without the app registering anything.
        if (typeInfo == null)
        {
            typeInfo = ((IJsonTypeInfoResolver)BuiltInJsonContext.Default).GetTypeInfo(type, this.Options);
            if (typeInfo != null)
                typeInfo = this.builtIns.GetOrAdd(type, typeInfo);
        }

        if (typeInfo == null)
            throw new InvalidOperationException(
                $"No JsonTypeInfo registered for type '{type.FullName}'. " +
                $"Register a JsonSerializerContext containing this type via services.AddJsonContext(...), " +
                $"add [ShinyJsonInclude] to opt in collection support, or call Shiny.Json.AddContext/AddResolver."
            );

        return typeInfo;
    }
}
