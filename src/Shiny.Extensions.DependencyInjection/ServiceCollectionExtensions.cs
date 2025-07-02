using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    // TODO: can below handle open generics somehow?
    
    /// <summary>
    /// This method registers a singleton service for the specified implementation type against all of the interfaces it implements.
    /// All instances returned will be the same instance for all interfaces
    /// </summary>
    /// <param name="services"></param>
    /// <param name="keyName">(optional) Registers all interfaces with keyname as well</param>
    /// <typeparam name="TImpl"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddSingletonAsImplementedInterfaces<TImpl>(this IServiceCollection services, string? keyName = null)
        where TImpl : class
    {
        if (keyName == null)
            services.AddSingleton<TImpl>();
        else
            services.AddKeyedSingleton(keyName, typeof(TImpl));
            
        var interfaces = typeof(TImpl)
            .GetInterfaces()
            .Where(x => x != typeof(IDisposable));

        foreach (var interfaceType in interfaces)
        {
            if (keyName == null)
                services.AddSingleton(interfaceType, sp => sp.GetRequiredService<TImpl>());
            else
                services.AddKeyedScoped(interfaceType, keyName, (sp, _) => sp.GetRequiredService<TImpl>());
        }

        return services;
    }
    
    
    /// <summary>
    /// This method registers a scoped service for the specified implementation type against all of the interfaces it implements.
    /// All instances returned will be the same instance for all interfaces within the same lifecycle scope
    /// </summary>
    /// <param name="services"></param>
    /// <param name="keyName">(optional) Registers all interfaces with keyname as well</param>
    /// <typeparam name="TImpl"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddScopedAsImplementedInterfaces<TImpl>(this IServiceCollection services, string? keyName = null)
        where TImpl : class
    {
        services.AddScoped<TImpl>();
        var implementationType = typeof(TImpl);
        var interfaces = implementationType
            .GetInterfaces()
            .Where(x => x != typeof(IDisposable));

        foreach (var interfaceType in interfaces)
        {
            if (keyName == null)
                services.AddSingleton(interfaceType, sp => sp.GetRequiredService<TImpl>());
            else
                services.AddKeyedScoped(interfaceType, keyName, (sp, _) => sp.GetRequiredService<TImpl>());
        }
        
        return services;
    }
    
    // /// <summary>
    // /// This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    // /// </summary>
    // /// <typeparam name="TImpl"></typeparam>
    // /// <param name="services"></param>
    // /// <returns></returns>
    // public static IServiceCollection AddShinyService<TImpl>(this IServiceCollection services) where TImpl : class
    //     => services.AddShinyService(typeof(TImpl));

    // /// <summary>
    // /// This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    // /// </summary>
    // /// <param name="implementationType"></param>
    // /// <param name="services"></param>
    // /// <returns></returns>
    // public static IServiceCollection AddShinyService(this IServiceCollection services, Type implementationType)
    // {
    //     services.AddShinyStores();
    //     services.AddSingleton(implementationType);
    //     
    //     var interfaces = implementationType
    //         .GetInterfaces()
    //         .Where(x => x != typeof(IDisposable))
    //         .ToList();
    //
    //     if (interfaces.Any(x => x == typeof(INotifyPropertyChanged)) || interfaces.Any(x => x == typeof(IShinyComponentStartup)))
    //     {
    //         services.AddSingleton(implementationType, sp =>
    //         {
    //             // var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ShinyStartup");
    //             var instance = ActivatorUtilities.CreateInstance(sp, implementationType);
    //
    //             if (instance is INotifyPropertyChanged npc)
    //             {
    //                 // logger.LogInformation("Startup Binding for " + fn);
    //
    //                 sp
    //                     .GetRequiredService<IObjectStoreBinder>()
    //                     .Bind(npc);
    //             }
    //
    //             if (instance is IShinyComponentStartup startup)
    //             {
    //                 // logger.LogInformation("Component Start: " + fn);
    //                 startup.ComponentStart();
    //             }
    //             return instance;
    //         });
    //         interfaces.Remove(typeof(INotifyPropertyChanged));
    //         interfaces.Remove(typeof(IShinyComponentStartup));
    //     }
    //     foreach (var iface in interfaces)
    //     { 
    //         services.AddSingleton(iface, sp => sp.GetRequiredService(implementationType));
    //     }
    //     return services;
    // }
}