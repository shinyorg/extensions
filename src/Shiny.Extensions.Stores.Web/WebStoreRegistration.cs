using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Repositories;
using Shiny.Extensions.Stores.Web;

namespace Shiny;


public static class WebStoreRegistration
{
    /// <summary>
    /// Registers an <see cref="IKeyValueStore"/> backed by the browser's localStorage,
    /// keyed under <see cref="StoreKeys.Default"/>, and exposes it as the unkeyed default.
    /// Consumers must include
    /// <c>&lt;script src="_content/Shiny.Extensions.Stores.Web/shiny-storage.js"&gt;&lt;/script&gt;</c>
    /// in their index.html before blazor.webassembly.js.
    /// </summary>
    public static IServiceCollection AddLocalStorageKeyValueStore(this IServiceCollection services)
    {
        services.AddShinyStores();
        services.AddKeyedSingleton<IKeyValueStore, LocalStorageKeyValueStore>(StoreKeys.Default);
        services.TryAddSingleton<IKeyValueStore>(sp => sp.GetRequiredKeyedService<IKeyValueStore>(StoreKeys.Default));
        return services;
    }


    /// <summary>
    /// Registers an <see cref="IRepository"/> backed by the browser's localStorage.
    /// Consumers must include
    /// <c>&lt;script src="_content/Shiny.Extensions.Stores.Web/shiny-storage.js"&gt;&lt;/script&gt;</c>
    /// in their index.html before blazor.webassembly.js.
    /// </summary>
    public static IServiceCollection AddLocalStorageRepository(this IServiceCollection services)
    {
        services.AddShinyStores();
        services.TryAddSingleton<IRepository, LocalStorageRepository>();
        return services;
    }
}
