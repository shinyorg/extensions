using CommunityToolkit.Mvvm.ComponentModel;
using Shiny.Extensions.Stores;
using Shouldly;

namespace Shiny.Extensions.Maui.Tests;


public class DependencyInjectionTests : IDisposable
{
    IServiceProvider? serviceProvider;
    
    [Theory(DisplayName = "Rebind - Reflection")]
    [InlineData("secure")]
    [InlineData("settings")]
    public void Rebind_Reflection(string storeType)
    {
        var services = new ServiceCollection();
        services.AddPersistentService<AppSettings>(storeType);
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
        appSettings2.RichObject.Hello.ShouldBe("World");
    }
    
    
    [Theory(DisplayName = "Rebind - Reflector")]
    [InlineData("secure")]
    [InlineData("settings")]
    public void Rebind_Reflector(string storeType)
    {
        var services = new ServiceCollection();
        services.AddPersistentService<AppSettings2>(storeType);
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
        appSettings2.RichObject.Hello.ShouldBe("World");
    }
    

    public void Dispose()
    {
        var stores = this.serviceProvider?.GetServices<IKeyValueStore>() ?? [];
        try
        {
            foreach (var store in stores)
                store.Clear();
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
    public string Hello { get; set; }
}


[Reflector]
partial class AppSettings2 : ObservableObject
{
    [ObservableProperty] bool isEnabled = true;
    [ObservableProperty] RichObject2? richObject;
}

[Reflector]
partial class RichObject2
{
    public string Hello { get; set; }
}