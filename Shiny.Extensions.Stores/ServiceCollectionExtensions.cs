using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Extensions.Stores.Impl;
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
#if PLATFORM
            services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
            services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
#endif
        }
        return services;
    }
}