using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Infrastructure;

namespace Shiny;


public static class StoreExtensions
{
    static readonly object syncLock = new object();

    /// <summary>
    /// If the value is null or default for the type, it will remove the key from the store - otherwise it will store it
    /// </summary>
    /// <param name="store"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void SetOrRemove(this IKeyValueStore store, string key, object? value)
    {
        if (value == null)
            store.Remove(key);
        else
            store.Set(key, value!);
    }

    /// <summary>
    /// Gets a generic object from the store
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="store"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T Get<T>(this IKeyValueStore store, string key, T defaultValue = default)
    {
        if (!store.Contains(key))
            return defaultValue;

        return (T)store.Get(typeof(T), key);
    }

    /// <summary>
    /// Thread safetied setting value incrementor
    /// </summary>
    /// <param name="store"></param>
    /// <returns></returns>
    public static int IncrementValue(this IKeyValueStore store, string key = "NextId")
    {
        var id = 0;

        lock (syncLock)
        {
            id = store.Get<int>(key);
            id++;
            store.Set(key, id);
        }
        return id;
    }

    /// <summary>
    /// Gets a required value from settings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="store"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T GetRequired<T>(this IKeyValueStore store, string key)
    {
        if (!store.Contains(key))
            throw new ArgumentException($"Store key '{key}' is not set");

        return store.Get<T>(key)!;
    }

    /// <summary>
    /// This will only set the value if the setting is not currently set.  Will not fire Changed event
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static bool SetDefault<T>(this IKeyValueStore store, string key, T value)
    {
        if (store.Contains(key))
            return false;

        store.Set(key, value);
        return true;
    }
    
    
    public static IServiceCollection AddShinyStores(this IServiceCollection services)
    {
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        
        if (!services.Any(x => x.ImplementationType == typeof(MemoryKeyValueStore)))
        {
            services.AddSingleton<IKeyValueStore, MemoryKeyValueStore>();
#if PLATFORM
            services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
            services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
#endif
        }
        return services;
    }


    /// <summary>
    ///  This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    /// </summary>
    /// <param name="services"></param>
    /// <param name="keyValueAlias">(optional) allows you to set the store to bind to</param>
    /// <typeparam name="TImpl"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddPersistentService<TImpl>(this IServiceCollection services, string? keyValueAlias = null) where TImpl : class, INotifyPropertyChanged
        => services.AddPersistentService(typeof(TImpl), keyValueAlias);
    
    /// <summary>
    /// This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    /// </summary>
    /// <param name="implementationType"></param>
    /// <param name="services"></param>
    /// <param name="keyValueAlias">(optional) allows you to set the store to bind to</param>
    /// <returns></returns>
    public static IServiceCollection AddPersistentService(this IServiceCollection services, Type implementationType, string? keyValueAlias = null)
    {
        services.AddShinyStores();
        services.AddSingleton(implementationType, sp =>
        {
            var instance = (INotifyPropertyChanged)ActivatorUtilities.CreateInstance(sp, implementationType);
            sp.GetRequiredService<IObjectStoreBinder>().Bind(instance, keyValueAlias); // TODO: object key?
            return instance;
        });
        var interfaces = implementationType
            .GetInterfaces()
            .Where(x => 
                x != typeof(IDisposable) &&
                x != typeof(INotifyPropertyChanged)
            )
            .ToList();
        
        foreach (var iface in interfaces)
            services.AddSingleton(iface, sp => sp.GetRequiredService(implementationType));

        return services;
    }
}
