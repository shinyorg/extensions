using Microsoft.Extensions.DependencyInjection;

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
}