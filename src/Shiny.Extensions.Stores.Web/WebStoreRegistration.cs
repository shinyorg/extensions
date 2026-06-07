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
    /// <remarks>
    /// Because the localStorage store requires <c>IJSRuntime</c>, the
    /// <see cref="Shiny.Stores"/> static accessor will only be wired up after the
    /// service provider is built. Call <c>host.Services.UseShinyStores()</c> once
    /// after <c>builder.Build()</c> so persistent-service property getters
    /// (which read <c>Shiny.Stores.Default</c>) resolve to the JS-backed store.
    /// </remarks>
    public static IServiceCollection AddLocalStorageKeyValueStore(this IServiceCollection services)
    {
        services.AddJsonSerialization();
        services.TryAddKeyedSingleton<IKeyValueStore, LocalStorageKeyValueStore>(StoreKeys.Default);
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
        services.AddJsonSerialization();
        services.TryAddSingleton<IRepository, LocalStorageRepository>();
        return services;
    }
}
