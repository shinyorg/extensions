using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection.Internals;

namespace Shiny.Extensions.DependencyInjection;


public static class ServiceProviderExtensions
{
    /// <summary>
    /// Register any source generated services across ALL libraries that use Shiny.Extensions.DependencyInjection registration
    /// </summary>
    /// <param name="services"></param>
    /// <param name="categories"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyServiceRegistry(this IServiceCollection services, params IEnumerable<string> categories)
    {
        ServiceRegistry.RunCallbacks(services, categories);
        return services;
    }
    
    
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
    /// Runs any registered startup tasks
    /// </summary>
    /// <param name="services"></param>
    public static void RunStartupTasks(this IServiceProvider services)
    {
        var startupTasks = services.GetServices<IShinyStartupTask>();
        foreach (var task in startupTasks)
            task.Start();
    }
}