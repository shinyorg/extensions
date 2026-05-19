using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.DependencyInjection.Tests;


public class ChainExtensionTests
{
    public interface IThing { }
    public class Thing : IThing { }


    [Fact]
    public void SingletonHookFiresOnce()
    {
        var fires = 0;
        var services = new ServiceCollection()
            .AddSingleton<IThing>(_ => new Thing())
            .OnResolved<IThing>((_, _) => fires++);

        var sp = services.BuildServiceProvider();
        var a = sp.GetRequiredService<IThing>();
        var b = sp.GetRequiredService<IThing>();

        Assert.Same(a, b);
        Assert.Equal(1, fires);
    }


    [Fact]
    public void TransientHookFiresPerResolve()
    {
        var fires = 0;
        var sp = new ServiceCollection()
            .AddTransient<IThing>(_ => new Thing())
            .OnResolved<IThing>((_, _) => fires++)
            .BuildServiceProvider();

        sp.GetRequiredService<IThing>();
        sp.GetRequiredService<IThing>();
        sp.GetRequiredService<IThing>();

        Assert.Equal(3, fires);
    }


    [Fact]
    public void ScopedHookFiresPerScope()
    {
        var fires = 0;
        var sp = new ServiceCollection()
            .AddScoped<IThing>(_ => new Thing())
            .OnResolved<IThing>((_, _) => fires++)
            .BuildServiceProvider();

        using (var s1 = sp.CreateScope())
        {
            s1.ServiceProvider.GetRequiredService<IThing>();
            s1.ServiceProvider.GetRequiredService<IThing>();
        }
        using (var s2 = sp.CreateScope())
        {
            s2.ServiceProvider.GetRequiredService<IThing>();
        }

        Assert.Equal(2, fires);
    }


    [Fact]
    public void KeyedSingletonPreservesKey()
    {
        var fires = 0;
        var sp = new ServiceCollection()
            .AddKeyedSingleton<IThing>("alpha", (_, _) => new Thing())
            .OnResolved<IThing>((_, _) => fires++)
            .BuildServiceProvider();

        var resolved = sp.GetRequiredKeyedService<IThing>("alpha");
        Assert.NotNull(resolved);
        Assert.Equal(1, fires);
        Assert.Null(sp.GetService<IThing>());
    }


    [Fact]
    public void OnResolvedWithWrongTypeThrows()
    {
        var services = new ServiceCollection().AddSingleton<IThing>(_ => new Thing());

        Assert.Throws<InvalidOperationException>(
            () => services.OnResolved<string>((_, _) => { })
        );
    }


    [Fact]
    public void OnResolvedOnEmptyCollectionThrows()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(
            () => services.OnResolved<IThing>((_, _) => { })
        );
    }


    [Fact]
    public void OnResolvedOnInstanceRegistrationThrows()
    {
        var services = new ServiceCollection().AddSingleton<IThing>(new Thing());

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.OnResolved<IThing>((_, _) => { })
        );
        Assert.Contains("factory-based", ex.Message);
    }


    [Fact]
    public void OnResolvedOnTypeBasedRegistrationThrows()
    {
        var services = new ServiceCollection().AddSingleton<IThing, Thing>();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.OnResolved<IThing>((_, _) => { })
        );
        Assert.Contains("factory-based", ex.Message);
    }


    [Fact]
    public void ActionOverloadFiresWithoutServiceProvider()
    {
        var fires = 0;
        var sp = new ServiceCollection()
            .AddSingleton<IThing>(_ => new Thing())
            .OnResolved<IThing>(_ => fires++)
            .BuildServiceProvider();

        sp.GetRequiredService<IThing>();
        Assert.Equal(1, fires);
    }


    [Fact]
    public void HookCanAccessServiceProvider()
    {
        var seen = false;
        var sp = new ServiceCollection()
            .AddSingleton("hello")
            .AddSingleton<IThing>(_ => new Thing())
            .OnResolved<IThing>((_, provider) =>
            {
                Assert.Equal("hello", provider.GetRequiredService<string>());
                seen = true;
            })
            .BuildServiceProvider();

        sp.GetRequiredService<IThing>();
        Assert.True(seen);
    }
}
