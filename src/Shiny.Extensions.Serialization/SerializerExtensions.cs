using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny;


public static class SerializerExtensions
{
    /// <summary>
    /// Registers <see cref="ISerializer"/> in DI, backed by the shared
    /// <see cref="Json.Default"/> instance. Idempotent.
    /// </summary>
    public static IServiceCollection AddJsonSerialization(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer>(_ => Json.Default);
        return services;
    }


    /// <summary>
    /// Adds a <see cref="JsonSerializerContext"/> to the shared serializer chain so its
    /// types can be (de)serialized in an AOT-compatible way. Ensures
    /// <see cref="ISerializer"/> is registered in DI.
    /// </summary>
    public static IServiceCollection AddJsonContext(this IServiceCollection services, JsonSerializerContext context)
    {
        Json.AddContext(context);
        return services.AddJsonSerialization();
    }


    /// <summary>
    /// Registers a callback that mutates the shared <see cref="JsonSerializerOptions"/>.
    /// Must be called before the serializer is first used.
    /// </summary>
    public static IServiceCollection ConfigureJsonSerializer(this IServiceCollection services, Action<JsonSerializerOptions> configure)
    {
        Json.Configure(configure);
        return services.AddJsonSerialization();
    }


    /// <summary>
    /// Replaces the shared serializer with a custom <see cref="ISerializer"/> instance AND
    /// registers it in DI. The replacement need not be JSON-based — any <see cref="ISerializer"/>
    /// (binary, MessagePack, XML, etc.) is accepted. Rare — most consumers should extend the
    /// built-in JSON serializer via <see cref="AddJsonContext"/>/<see cref="ConfigureJsonSerializer"/>.
    ///
    /// Must be called before first access to <see cref="Json.Default"/> or any consumer that
    /// captures the serializer in its constructor (e.g. <see cref="Shiny.Stores.Default"/>).
    /// </summary>
    public static IServiceCollection AddSerializer(this IServiceCollection services, ISerializer serializer)
    {
        if (serializer is null) throw new ArgumentNullException(nameof(serializer));
        Json.SetDefault(serializer);
        services.AddSingleton<ISerializer>(serializer);
        return services;
    }


    /// <summary>
    /// Registers <typeparamref name="TSerializer"/> as the DI <see cref="ISerializer"/> singleton.
    /// The serializer is constructed lazily by the container so it may take constructor-injected
    /// dependencies. Call <see cref="UseSerializer"/> after building the provider to snapshot the
    /// resolved instance into the static <see cref="Json.Default"/> accessor.
    /// </summary>
    public static IServiceCollection AddSerializer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer
    >(this IServiceCollection services)
        where TSerializer : class, ISerializer
    {
        services.AddSingleton<ISerializer, TSerializer>();
        return services;
    }


    /// <summary>
    /// For serializers whose construction depends on the service provider: resolves the
    /// DI-registered <see cref="ISerializer"/> and installs it as <see cref="Json.Default"/>.
    /// Mirrors <c>UseShinyStores</c> — call after the service provider is built and before
    /// any consumer that captures the static serializer.
    ///
    /// Pair with <see cref="AddSerializer{TSerializer}"/> or
    /// <c>services.AddSingleton&lt;ISerializer, MyImpl&gt;()</c> when <c>MyImpl</c> needs
    /// constructor-injected dependencies.
    /// </summary>
    public static IServiceProvider UseSerializer(this IServiceProvider services)
    {
        var s = services.GetRequiredService<ISerializer>();
        Json.SetDefault(s);
        return services;
    }
}
