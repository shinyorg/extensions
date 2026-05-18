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
    /// Registers Shiny store services: <see cref="ISerializer"/>, <see cref="IObjectStoreBinder"/>,
    /// platform-native keyed <see cref="IKeyValueStore"/> for <see cref="StoreKeys.Default"/> and
    /// <see cref="StoreKeys.Secure"/> (on mobile/desktop platforms), and an unkeyed default that
    /// resolves to the <see cref="StoreKeys.Default"/> store.
    /// </summary>
    public static IServiceCollection AddShinyStores(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();

#if PLATFORM
        if (!services.Any(x => x.ServiceType == typeof(IKeyValueStore) && x.ServiceKey?.Equals(StoreKeys.Default) == true))
        {
            services.AddKeyedSingleton<IKeyValueStore, SettingsKeyValueStore>(StoreKeys.Default);
            services.AddKeyedSingleton<IKeyValueStore, SecureKeyValueStore>(StoreKeys.Secure);
        }
#endif

        services.TryAddSingleton<IKeyValueStore>(sp =>
        {
            var settings = sp.GetKeyedService<IKeyValueStore>(StoreKeys.Default);
            return settings ?? new MemoryKeyValueStore();
        });

        return services;
    }


    /// <summary>
    /// Chains a binding step onto the most recently registered service. On first resolve the instance is
    /// bound to the object store via <see cref="IObjectStoreBinder"/>.
    /// </summary>
    /// <remarks>
    /// The preceding registration's <c>ServiceType</c> must implement <see cref="INotifyPropertyChanged"/>
    /// and must be factory-based - see <see cref="DIExtensions.OnResolved{TService}"/>.
    /// </remarks>
    /// <param name="services"></param>
    /// <param name="storeKey">(optional) DI service key of the target <see cref="IKeyValueStore"/></param>
    public static IServiceCollection BindOnResolve(this IServiceCollection services, object? storeKey = null)
    {
        services.AddShinyStores();
        return services.OnResolved<INotifyPropertyChanged>((instance, sp) =>
            sp.GetRequiredService<IObjectStoreBinder>().Bind(instance, storeKey)
        );
    }


    /// <summary>
    /// Registers a singleton service backed by a user-supplied factory, binds it to a
    /// keyed <see cref="IKeyValueStore"/> via <see cref="IObjectStoreBinder"/> on first resolve,
    /// and registers the same instance for every interface the implementation declares.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="factory">factory used to construct <typeparamref name="TImpl"/> (kept AOT-clean by avoiding reflection)</param>
    /// <param name="storeKey">(optional) DI service key of the target <see cref="IKeyValueStore"/></param>
    public static IServiceCollection AddPersistentService<TImpl>(
        this IServiceCollection services,
        Func<IServiceProvider, TImpl> factory,
        object? storeKey = null
    ) where TImpl : class, INotifyPropertyChanged
    {
        services.AddShinyStores();
        services.AddSingleton<TImpl>(factory);
        services.OnResolved<TImpl>((instance, sp) =>
            sp.GetRequiredService<IObjectStoreBinder>().Bind(instance, storeKey)
        );

        var interfaces = typeof(TImpl)
            .GetInterfaces()
            .Where(x =>
                x != typeof(IDisposable) &&
                x != typeof(INotifyPropertyChanged)
            );

        foreach (var iface in interfaces)
            services.AddSingleton(iface, sp => sp.GetRequiredService<TImpl>());

        return services;
    }
}
