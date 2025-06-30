using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Impl;

namespace Shiny.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSingletonAsImplementedInterfaces<TImpl>(this IServiceCollection services)
        where TImpl : class
    {
        var implementationType = typeof(TImpl);
        var interfaces = implementationType.GetInterfaces();
        
        foreach (var interfaceType in interfaces)
        {
            services.AddSingleton(interfaceType, implementationType);
        }
        
        return services;
    }
    
    
    public static IServiceCollection AddScopedAsImplementedInterfaces<TImpl>(this IServiceCollection services)
        where TImpl : class
    {
        var implementationType = typeof(TImpl);
        var interfaces = implementationType.GetInterfaces();
        
        foreach (var interfaceType in interfaces)
        {
            services.AddScoped(interfaceType, implementationType);
        }
        
        return services;
    }
    
    
    public static IServiceCollection AddTransientAsImplementedInterfaces<TImpl>(this IServiceCollection services)
        where TImpl : class
    {
        var implementationType = typeof(TImpl);
        var interfaces = implementationType.GetInterfaces();
        
        foreach (var interfaceType in interfaces)
        {
            services.AddTransient(interfaceType, implementationType);
        }
        
        return services;
    }
    
    /// <summary>
    /// This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    /// </summary>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService<TImpl>(this IServiceCollection services) where TImpl : class
        => services.AddShinyService(typeof(TImpl));

    /// <summary>
    /// This will add the implementation for ALL of its interfaces and create a persistent storage binding if INotifyPropertyChanged is implemented
    /// </summary>
    /// <param name="implementationType"></param>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyService(this IServiceCollection services, Type implementationType)
    {
        services.AddShinyStores();
        services.AddSingleton(implementationType);
        
        var interfaces = implementationType
            .GetInterfaces()
            .Where(x => x != typeof(IDisposable))
            .ToList();

        if (interfaces.Any(x => x == typeof(INotifyPropertyChanged)) || interfaces.Any(x => x == typeof(IShinyComponentStartup)))
        {
            services.AddSingleton(implementationType, sp =>
            {
                // var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ShinyStartup");
                var instance = ActivatorUtilities.CreateInstance(sp, implementationType);

                if (instance is INotifyPropertyChanged npc)
                {
                    // logger.LogInformation("Startup Binding for " + fn);

                    sp
                        .GetRequiredService<IObjectStoreBinder>()
                        .Bind(npc);
                }

                if (instance is IShinyComponentStartup startup)
                {
                    // logger.LogInformation("Component Start: " + fn);
                    startup.ComponentStart();
                }
                return instance;
            });
            interfaces.Remove(typeof(INotifyPropertyChanged));
            interfaces.Remove(typeof(IShinyComponentStartup));
        }
        foreach (var iface in interfaces)
        { 
            services.AddSingleton(iface, sp => sp.GetRequiredService(implementationType));
        }
        return services;
    }
}