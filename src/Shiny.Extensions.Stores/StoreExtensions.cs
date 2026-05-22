using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Infrastructure;

namespace Shiny;


public static class StoreExtensions
{
    static readonly object syncLock = new();


    /// <summary>
    /// Gets a value, returning <paramref name="defaultValue"/> if the key is absent.
    /// </summary>
    public static T Get<T>(this IKeyValueStore store, string key, T defaultValue)
    {
        var value = store.Get<T>(key);
        return value is null ? defaultValue : value;
    }


    /// <summary>
    /// Removes the key when <paramref name="value"/> is null or equal to <c>default(T)</c>;
    /// otherwise stores the value.
    /// </summary>
    public static void SetOrRemove<T>(this IKeyValueStore store, string key, T? value)
    {
        if (value is null || value.Equals(default(T)))
            store.Remove(key);
        else
            store.Set(key, value);
    }


    /// <summary>
    /// Thread-safe incrementing counter stored at the given key.
    /// </summary>
    public static int IncrementValue(this IKeyValueStore store, string key = "NextId")
    {
        lock (syncLock)
        {
            var id = store.Get<int>(key) + 1;
            store.Set(key, id);
            return id;
        }
    }


    /// <summary>
    /// Gets a required value. Throws if the key is not set.
    /// </summary>
    public static T GetRequired<T>(this IKeyValueStore store, string key)
    {
        var value = store.Get<T>(key);
        if (value is null)
            throw new ArgumentException($"Store key '{key}' is not set");

        return value;
    }


    /// <summary>
    /// Sets a value only if the key is not already present. Returns true if the value was set.
    /// </summary>
    public static bool SetDefault<T>(this IKeyValueStore store, string key, T value)
    {
        if (store.Contains(key))
            return false;

        store.Set(key, value);
        return true;
    }


    /// <summary>
    /// Registers Shiny store services: <see cref="ISerializer"/> and platform-native keyed
    /// <see cref="IKeyValueStore"/> for <see cref="StoreKeys.Default"/> and <see cref="StoreKeys.Secure"/>.
    /// After building the service provider, call <see cref="UseShinyStores(IServiceProvider)"/>
    /// (or a host-specific overload) to wire up the <see cref="Stores"/> static accessor.
    /// </summary>
    public static IServiceCollection AddShinyStores(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();

#if PLATFORM
        if (!services.Any(x => x.ServiceType == typeof(IKeyValueStore) && x.ServiceKey?.Equals(StoreKeys.Default) == true))
        {
            services.AddKeyedSingleton<IKeyValueStore, SettingsKeyValueStore>(StoreKeys.Default);
            services.AddKeyedSingleton<IKeyValueStore, SecureKeyValueStore>(StoreKeys.Secure);
        }
#else
        services.TryAddKeyedSingleton<IKeyValueStore, MemoryKeyValueStore>(StoreKeys.Default);
        services.TryAddKeyedSingleton<IKeyValueStore, MemoryKeyValueStore>(StoreKeys.Secure);
#endif
        return services;
    }


    /// <summary>
    /// Initializes the <see cref="Stores"/> static accessor with the given service provider.
    /// Call this once after the service provider is built (e.g. after <c>host.Build()</c>,
    /// <c>builder.Build()</c>, or <c>services.BuildServiceProvider()</c>).
    /// </summary>
    public static IServiceProvider UseShinyStores(this IServiceProvider services)
    {
        Stores.Initialize(services);
        return services;
    }
}
