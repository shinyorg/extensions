using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.Stores;

namespace Shiny;


/// <summary>
/// Static accessor for the keyed <see cref="IKeyValueStore"/> registrations created by
/// <see cref="StoreExtensions.AddShinyStores"/>. Initialize once after the service provider
/// is built by calling <see cref="StoreExtensions.UseShinyStores(IServiceProvider)"/>,
/// or directly via <see cref="Initialize"/>.
/// </summary>
public static class Stores
{
    static IServiceProvider? sp;

    /// <summary>The default settings store (keyed <see cref="StoreKeys.Default"/>).</summary>
    public static IKeyValueStore Default => Resolve(StoreKeys.Default);

    /// <summary>The secure/encrypted store (keyed <see cref="StoreKeys.Secure"/>).</summary>
    public static IKeyValueStore Secure => Resolve(StoreKeys.Secure);

    /// <summary>Resolves a store by arbitrary DI service key.</summary>
    public static IKeyValueStore Keyed(object key) => Resolve(key);

    /// <summary>
    /// Sets the underlying <see cref="IServiceProvider"/> used to resolve stores.
    /// Prefer calling <see cref="StoreExtensions.UseShinyStores(IServiceProvider)"/>,
    /// which delegates here.
    /// </summary>
    public static void Initialize(IServiceProvider provider) => sp = provider;

    static IKeyValueStore Resolve(object key) =>
        (sp ?? throw new InvalidOperationException(
            "Shiny stores not initialized. After calling AddShinyStores() on your services, " +
            "call serviceProvider.UseShinyStores() once the provider is built " +
            "(e.g. host.Services.UseShinyStores() after host.Build())."
        )).GetRequiredKeyedService<IKeyValueStore>(key);
}
