using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.Stores;

namespace Shiny;


/// <summary>
/// Static accessor for the keyed <see cref="IKeyValueStore"/> registrations created by
/// <see cref="StoreExtensions.AddShinyStores"/>. Initialized at app startup via the
/// hosted <c>StoresInitializer</c>, or manually via <see cref="Initialize"/>.
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
    /// Called automatically by the hosted initializer; tests and host-less apps may call this directly.
    /// </summary>
    public static void Initialize(IServiceProvider provider) => sp = provider;

    static IKeyValueStore Resolve(object key) =>
        (sp ?? throw new InvalidOperationException(
            "Shiny stores not initialized. Ensure AddShinyStores() is called and either run a host (IHostedService) or call Stores.Initialize(serviceProvider) manually."
        )).GetRequiredKeyedService<IKeyValueStore>(key);
}
