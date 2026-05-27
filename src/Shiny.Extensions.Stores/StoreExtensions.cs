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
    /// Registers Shiny store services into DI: the shared <see cref="ISerializer"/>
    /// (<see cref="Stores.Serializer"/>) and keyed <see cref="IKeyValueStore"/> for
    /// <see cref="StoreKeys.Default"/> and <see cref="StoreKeys.Secure"/>. The DI
    /// registrations resolve to the same singletons that <see cref="Stores.Default"/>
    /// and <see cref="Stores.Secure"/> return, so static and DI consumers share state.
    /// </summary>
    /// <remarks>
    /// No post-build <c>UseShinyStores</c> call is required for mobile/desktop —
    /// the static accessor self-bootstraps on first access. <c>UseShinyStores</c>
    /// remains available for scenarios where a store must be constructed from the
    /// service provider (e.g. Blazor's <c>LocalStorageKeyValueStore</c> needs
    /// <c>IJSRuntime</c>) so it can be snapshotted into the static.
    /// </remarks>
    public static IServiceCollection AddShinyStores(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer>(Stores.Serializer);

        services.TryAddKeyedSingleton<IKeyValueStore>(
            StoreKeys.Default,
            (_, _) => Stores.Default
        );
        services.TryAddKeyedSingleton<IKeyValueStore>(
            StoreKeys.Secure,
            (_, _) => Stores.Secure
        );
        return services;
    }


    /// <summary>
    /// Snapshots keyed <see cref="IKeyValueStore"/> registrations from the built
    /// service provider into the <see cref="Stores"/> static accessor. Optional on
    /// mobile/desktop (the static self-bootstraps); needed when a store can only be
    /// constructed via DI — for example Blazor's <c>LocalStorageKeyValueStore</c>,
    /// which depends on <c>IJSRuntime</c>.
    /// </summary>
    public static IServiceProvider UseShinyStores(this IServiceProvider services)
    {
        Stores.Initialize(services);
        return services;
    }
}
