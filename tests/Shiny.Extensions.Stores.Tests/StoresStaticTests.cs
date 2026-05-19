using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.Stores.Tests;


public class StoresStaticTests
{
    static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var serializer = StoreTests.CreateSerializer();
        services.AddSingleton<ISerializer>(serializer);
        services.AddKeyedSingleton<IKeyValueStore>(StoreKeys.Default, (_, _) => new MemoryKeyValueStore());
        services.AddKeyedSingleton<IKeyValueStore>(StoreKeys.Secure, (_, _) => new MemoryKeyValueStore());
        return services.BuildServiceProvider();
    }


    [Fact(DisplayName = "Stores Static - Default resolves keyed Default")]
    public void DefaultResolvesDefault()
    {
        Shiny.Stores.Initialize(BuildProvider());
        Shiny.Stores.Default.ShouldNotBeNull();
    }


    [Fact(DisplayName = "Stores Static - Secure resolves keyed Secure")]
    public void SecureResolvesSecure()
    {
        Shiny.Stores.Initialize(BuildProvider());
        Shiny.Stores.Secure.ShouldNotBeNull();
        Shiny.Stores.Secure.ShouldNotBeSameAs(Shiny.Stores.Default);
    }


    [Fact(DisplayName = "Stores Static - Round-trip via Default")]
    public void RoundTrip()
    {
        Shiny.Stores.Initialize(BuildProvider());
        Shiny.Stores.Default.Set("k1", "hello");
        Shiny.Stores.Default.Get<string>("k1").ShouldBe("hello");
    }
}
