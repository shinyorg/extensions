using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.DependencyInjection;


public static class ServiceProviderExtensions
{
    /// <summary>
    /// Checks if a service collection has a service registered for the specified type
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static bool HasService<TService>(this IServiceCollection services)
        => services.HasService(typeof(TService));

    /// <summary>
    /// Checks if a service collection has a service registered for the specified type
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    public static bool HasService(this IServiceCollection services, Type serviceType)
        => services.Any(x => x.ServiceType == serviceType);

    /// <summary>
    /// Checks if a service collection has an implementation registered for the specified type
    /// </summary>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static bool HasImplementation<TImpl>(this IServiceCollection services)
        => services.HasImplementation(typeof(TImpl));

    /// <summary>
    /// Checks if a service collection has an implementation registered for the specified type
    /// </summary>
    /// <param name="services"></param>
    /// <param name="implementationType"></param>
    /// <returns></returns>
    public static bool HasImplementation(this IServiceCollection services, Type implementationType)
        => services.Any(x => x.ServiceKey == null && x.ImplementationType == implementationType);


    /// <summary>
    /// Lazily resolves a service - helps in prevent resolve loops with delegates/services internal to Shiny
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="required"></param>
    /// <returns></returns>
    public static Lazy<T> GetLazyService<T>(this IServiceProvider services, bool required = false)
        => new(() => required ? services.GetRequiredService<T>() : services.GetService<T>());


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
        var interfaces = implementationType
            .GetInterfaces()
            .Where(x => x != typeof(IDisposable))
            .ToList();

        services.AddSingleton(implementationType);

        if (interfaces.Any(x => x == typeof(INotifyPropertyChanged)) || interfaces.Any(x => x == typeof(IShinyComponentStartup)))
        {
            services.AddSingleton(implementationType, services =>
            {
                //var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ShinyStartup");
                var instance = ActivatorUtilities.CreateInstance(services, implementationType);
                var fn = implementationType.FullName;

                if (instance is INotifyPropertyChanged npc)
                {
                    // logger.LogInformation("Startup Binding for " + fn);
                    //
                    // services
                    //     .GetRequiredService<IObjectStoreBinder>()
                    //     .Bind(npc);
                }

                if (instance is IShinyComponentStartup startup)
                {
                    //logger.LogInformation("Component Start: " + fn);
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