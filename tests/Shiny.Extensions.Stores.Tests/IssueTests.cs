using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.Stores.Tests;


public class IssueTests
{
    [Fact]
    public void Issue_1()
    {
        var memoryStore = new MemoryKeyValueStore();
        
        var services = new ServiceCollection();
        services.AddSingleton<IKeyValueStore>(_ => memoryStore);
        services.AddPersistentService<AppSettings>("memory");
        var sp = services.BuildServiceProvider();
        
        // TODO: I need a true local persistent store
        var appSettings = sp.GetRequiredService<AppSettings>();
        appSettings.IsEnabled.ShouldBeTrue("Default value should be true");
        appSettings.RichObject.ShouldBeNull("Default value should be null");
        appSettings.IsEnabled = false;
        appSettings.RichObject = new() { Hello = "World" };

        sp = services.BuildServiceProvider();
        var appSettings2 = sp.GetRequiredService<AppSettings>();
        appSettings2.IsEnabled.ShouldBeFalse("Value should have been restored as false");
        appSettings2.RichObject.ShouldNotBeNull("RichObject should have been restored");
        appSettings2.RichObject.Hello.ShouldBe("World");
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