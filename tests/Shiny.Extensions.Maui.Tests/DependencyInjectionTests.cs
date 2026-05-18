using CommunityToolkit.Mvvm.ComponentModel;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Infrastructure;
using Shouldly;

namespace Shiny.Extensions.Maui.Tests;


public class DependencyInjectionTests : IDisposable
{
    IServiceProvider? serviceProvider;


    static IServiceCollection NewServices()
    {
        var services = new ServiceCollection();

        // Ensure ISerializer with a reflection resolver so Maui tests don't need an AOT context
        var serializer = new DefaultSerializer();
        serializer.Options.TypeInfoResolverChain.Add(new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());
        services.AddSingleton<ISerializer>(serializer);

        return services;
    }


    [Theory(DisplayName = "Rebind - Reflection")]
    [InlineData(StoreKeys.Secure)]
    [InlineData(StoreKeys.Default)]
    public void Rebind_Reflection(string storeKey)
    {
        var services = NewServices();
        services.AddPersistentService<AppSettings>(_ => new AppSettings(), storeKey);
        this.serviceProvider = services.BuildServiceProvider();

        var appSettings = this.serviceProvider.GetRequiredService<AppSettings>();
        appSettings.IsEnabled.ShouldBeTrue("Default value should be true");
        appSettings.RichObject.ShouldBeNull("Default value should be null");
        appSettings.IsEnabled = false;
        appSettings.RichObject = new() { Hello = "World" };

        this.serviceProvider = services.BuildServiceProvider();
        var appSettings2 = this.serviceProvider.GetRequiredService<AppSettings>();
        appSettings2.IsEnabled.ShouldBeFalse("Value should have been restored as false");
        appSettings2.RichObject.ShouldNotBeNull("RichObject should have been restored");
        appSettings2.RichObject!.Hello.ShouldBe("World");
    }


    [Theory(DisplayName = "Rebind - Reflector")]
    [InlineData(StoreKeys.Secure)]
    [InlineData(StoreKeys.Default)]
    public void Rebind_Reflector(string storeKey)
    {
        var services = NewServices();
        services.AddPersistentService<AppSettings2>(_ => new AppSettings2(), storeKey);
        this.serviceProvider = services.BuildServiceProvider();

        var appSettings = this.serviceProvider.GetRequiredService<AppSettings2>();
        appSettings.IsEnabled.ShouldBeTrue("Default value should be true");
        appSettings.RichObject.ShouldBeNull("Default value should be null");
        appSettings.IsEnabled = false;
        appSettings.RichObject = new() { Hello = "World" };

        this.serviceProvider = services.BuildServiceProvider();
        var appSettings2 = this.serviceProvider.GetRequiredService<AppSettings2>();
        appSettings2.IsEnabled.ShouldBeFalse("Value should have been restored as false");
        appSettings2.RichObject.ShouldNotBeNull("RichObject should have been restored");
        appSettings2.RichObject!.Hello.ShouldBe("World");
    }


    public void Dispose()
    {
        if (this.serviceProvider == null) return;

        try
        {
            foreach (var key in new[] { StoreKeys.Default, StoreKeys.Secure })
            {
                var store = this.serviceProvider.GetKeyedService<IKeyValueStore>(key);
                store?.Clear();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}


partial class AppSettings : ObservableObject
{
    [ObservableProperty] bool isEnabled = true;
    [ObservableProperty] RichObject? richObject;
}


class RichObject
{
    public string Hello { get; set; } = string.Empty;
}


[Reflector]
partial class AppSettings2 : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial RichObject2? RichObject { get; set; }
}


[Reflector]
partial class RichObject2
{
    public string Hello { get; set; } = string.Empty;
}
