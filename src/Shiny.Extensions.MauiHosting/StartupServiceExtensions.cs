using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Shiny.Impl;

namespace Shiny;

public static class StartupServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IStartupService"/> for installing the app into the desktop operating system's
    /// startup (launch at login) list on Windows, macOS, and Linux.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure">Optional inline configuration of <see cref="StartupServiceOptions"/>.</param>
    public static IServiceCollection AddStartupService(
        this IServiceCollection services,
        Action<StartupServiceOptions>? configure = null
    )
    {
        if (configure != null)
            services.Configure(configure);

        services.TryAddSingleton<IStartupService, StartupService>();
        return services;
    }

    /// <summary>
    /// MAUI overload of <see cref="AddStartupService(IServiceCollection, Action{StartupServiceOptions})"/>.
    /// A macOS (AppKit) app has no <see cref="MauiAppBuilder"/> - register against its
    /// <see cref="IServiceCollection"/> instead.
    /// </summary>
    public static MauiAppBuilder AddStartupService(
        this MauiAppBuilder builder,
        Action<StartupServiceOptions>? configure = null
    )
    {
        builder.Services.AddStartupService(configure);
        return builder;
    }
}
