using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.Stores.Web;

namespace Shiny.Extensions.Stores;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShinyWebAssemblyStores(this IServiceCollection services)
    {
        services.AddShinyStores();
        
        if (!services.Any(x => x.ImplementationType == typeof(SessionStorageKeyValueStore)))
        {
            services.AddSingleton<IKeyValueStore, LocalStorageKeyValueStore>();
            services.AddSingleton<IKeyValueStore, SessionStorageKeyValueStore>();
        }
        return services;
    }
}