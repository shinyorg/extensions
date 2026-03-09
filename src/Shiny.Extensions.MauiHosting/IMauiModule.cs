using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Shiny;

public interface IMauiModule
{
    /// <summary>
    /// Add any services to the MauiAppBuilder. This is called before the app is built, so you can add services here that will be available in the service provider when the app is built. This is also where you can add things like Shiny which need to be added to the MauiAppBuilder to work properly.
    /// </summary>
    /// <param name="builder"></param>
    void Add(MauiAppBuilder builder);
    
    /// <summary>
    /// Runs after the app is built and the service provider is available. This is useful for things like Shiny which need to resolve services at startup
    /// DO NOT BLOCK HERE - this is not async and will block the app from starting if you do. If you need to do async work, use Task.Run or something similar to run it on a background thread
    /// </summary>
    /// <param name="app"></param>
    void Use(IPlatformApplication app);
}