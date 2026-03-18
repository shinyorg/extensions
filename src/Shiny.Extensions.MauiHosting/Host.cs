using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Shiny;

public class Host : IMauiInitializeService
{
    public static IServiceProvider Services
    {
        get
        {
            if (field == null)
                throw new InvalidOperationException("Host isn't initialized yet");
            
            return field;
        }
        private set => field = value;
    }
    
#if APPLE || ANDROID || WINDOWS

    internal static ILifecycleExecutor Lifecycle
    {
        get
        {
            if (field == null)
                throw new InvalidOperationException("Host isn't initialized yet");
            
            return field;
        }
        private set => field = value;
    }
    
#endif
    
    internal static List<IMauiModule> Modules { get; } = new();


    
    public void Initialize(IServiceProvider services)
    {
        var app = IPlatformApplication.Current!;
        Services = services;
#if APPLE || ANDROID || WINDOWS
        Lifecycle = services.GetRequiredService<ILifecycleExecutor>();
#endif        
        foreach (var module in Modules)
            module.Use(app);

        Modules.Clear();
    }
}