using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Extensions.Stores.Infrastructure;

namespace Shiny.Extensions.Stores;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShinyStores(
        this IServiceCollection services
    )
    {
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
    /// <typeparam name="TImpl"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddPersistentService<TImpl>(this IServiceCollection services, string? keyValueAlias = null) where TImpl : class, INotifyPropertyChanged
        => services.AddPersistentService(typeof(TImpl), keyValueAlias);
    
    /// <summary>
    /// This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    /// </summary>
    /// <param name="implementationType"></param>
    /// <param name="services"></param>
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