using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using Shiny.Impl;

namespace Shiny;

public static class BlazorHostingExtensions
{
    /// <summary>
    /// Registers <see cref="IAppSupport"/> for browser/device info and culture / time-zone
    /// change notifications in a Blazor WebAssembly app.
    /// Consumers must include
    /// <c>&lt;script src="_content/Shiny.Extensions.BlazorHosting/shiny-appsupport.js"&gt;&lt;/script&gt;</c>
    /// in their index.html before blazor.webassembly.js.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="appVersion">
    /// The head application's version. Pass a compile-time constant such as
    /// <c>ThisAssembly.AssemblyVersion</c> so no runtime reflection is required.
    /// </param>
    public static IServiceCollection AddAppSupport(this IServiceCollection services, Version appVersion)
    {
        services.TryAddSingleton<IAppSupport>(sp => new AppSupport(
            sp.GetRequiredService<IJSRuntime>(),
            appVersion
        ));
        return services;
    }

    /// <summary>
    /// Convenience overload for <see cref="AddAppSupport(IServiceCollection, Version)"/> that parses
    /// the supplied version string (e.g. <c>ThisAssembly.AssemblyVersion</c>).
    /// </summary>
    public static IServiceCollection AddAppSupport(this IServiceCollection services, string appVersion)
        => services.AddAppSupport(Version.Parse(appVersion));
}
