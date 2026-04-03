using Microsoft.AspNetCore.Builder;

namespace Shiny;

public static class WebHostingExtensions
{
    static readonly List<IWebModule> AllModules = new();
    
    /// <summary>
    ///  Adds the specified modules to the MauiAppBuilder. This will call the Add method on each module, allowing them to add services to the MauiAppBuilder. It will also add a singleton of IMauiInitializeService that will call the Use method on each module when the app is initialized. This allows you to do things like initialize Shiny or other services that need to be initialized at startup.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="modules"></param>
    /// <returns></returns>
    public static WebApplicationBuilder AddInfrastructureModules(
        this WebApplicationBuilder builder,
        params IEnumerable<IWebModule> modules
    )
    {
        foreach (var module in modules)
        {
            module.Add(builder);
            AllModules.Add(module);
        }
        return builder;
    }
    
    
    /// <summary>
    /// Calls the Use method on each module, allowing them to configure the WebApplication. This should be called after the app is built and before it is run. This allows you to do things like initialize Shiny or other services that need to be initialized at startup.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseInfrastructureModules(this WebApplication app)
    {
        foreach (var module in AllModules)
            module.Use(app);

        AllModules.Clear();
        return app;
    }
}