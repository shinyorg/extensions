using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.Stores.Tests;


public class StoresStaticTests : IDisposable
{
    public StoresStaticTests() => Shiny.Stores.Reset();
    public void Dispose() => Shiny.Stores.Reset();


    [Fact(DisplayName = "Stores Static - Default self-bootstraps")]
    public void DefaultSelfBootstraps()
    {
        Shiny.Stores.Default.ShouldNotBeNull();
    }


    [Fact(DisplayName = "Stores Static - Secure self-bootstraps and differs from Default")]
    public void SecureSelfBootstraps()
    {
        Shiny.Stores.Secure.ShouldNotBeNull();
        Shiny.Stores.Secure.ShouldNotBeSameAs(Shiny.Stores.Default);
    }


    [Fact(DisplayName = "Stores Static - Round-trip via Default")]
    public void RoundTrip()
    {
        Shiny.Stores.Default.Set("k1", "hello");
        Shiny.Stores.Default.Get<string>("k1").ShouldBe("hello");
    }


    [Fact(DisplayName = "Stores Static - Register overrides Default")]
    public void RegisterOverridesDefault()
    {
        var custom = new MemoryKeyValueStore();
        Shiny.Stores.Register(StoreKeys.Default, custom);
        Shiny.Stores.Default.ShouldBeSameAs(custom);
    }


    [Fact(DisplayName = "Stores Static - Keyed throws for unknown key")]
    public void KeyedThrowsForUnknown()
    {
        Should.Throw<KeyNotFoundException>(() => Shiny.Stores.Keyed("does-not-exist"));
    }


    [Fact(DisplayName = "Stores Static - Keyed returns registered custom key")]
    public void KeyedReturnsRegistered()
    {
        var custom = new MemoryKeyValueStore();
        Shiny.Stores.Register("my-key", custom);
        Shiny.Stores.Keyed("my-key").ShouldBeSameAs(custom);
    }


    [Fact(DisplayName = "Stores Static - Initialize snapshots from DI")]
    public void InitializeSnapshotsFromDi()
    {
        var fromDi = new MemoryKeyValueStore();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IKeyValueStore>(StoreKeys.Default, (_, _) => fromDi);
        var provider = services.BuildServiceProvider();

        Shiny.Stores.Initialize(provider);
        Shiny.Stores.Default.ShouldBeSameAs(fromDi);
    }


    [Fact(DisplayName = "AddShinyStores - DI keyed store returns same instance as static")]
    public void AddShinyStoresShareInstance()
    {
        var services = new ServiceCollection();
        services.AddShinyStores();
        var provider = services.BuildServiceProvider();

        var fromDi = provider.GetRequiredKeyedService<IKeyValueStore>(StoreKeys.Default);
        fromDi.ShouldBeSameAs(Shiny.Stores.Default);
    }


    [Fact(DisplayName = "AddShinyStores - ISerializer is the shared Stores.Serializer")]
    public void AddShinyStoresSerializerShared()
    {
        var services = new ServiceCollection();
        services.AddShinyStores();
        var provider = services.BuildServiceProvider();

        var fromDi = provider.GetRequiredService<ISerializer>();
        fromDi.ShouldBeSameAs(Shiny.Stores.Serializer);
    }
}
