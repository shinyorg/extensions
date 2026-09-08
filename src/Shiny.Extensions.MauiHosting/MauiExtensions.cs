using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Shiny.Impl;
#if MACOS
using Microsoft.Maui.Platforms.MacOS.Essentials;
#endif

namespace Shiny;

public static class MauiHostingExtensions
{
    /// <summary>
    /// Registers IMauiModules and wires Host's Initialize callback so each module's Use method fires at app startup.
    /// </summary>
    public static MauiAppBuilder AddInfrastructureModules(
        this MauiAppBuilder builder,
        params IEnumerable<IMauiModule> modules
    )
    {
        foreach (var module in modules)
        {
            module.Add(builder);
            ShinyHost.Modules.Add(module);
        }

        if (!builder.Services.HasImplementation<ShinyHost>())
            builder.Services.AddSingleton<IMauiInitializeService, ShinyHost>();

        return builder;
    }

    /// <summary>
    /// Registers <see cref="IAppSupport"/> for device info, browser/map launch helpers,
    /// and orientation / culture / time-zone change notifications.
    /// </summary>
    public static MauiAppBuilder AddAppSupport(this MauiAppBuilder builder)
    {
        builder.EnsureEssentials();
        builder.Services.TryAddSingleton<IAppSupport, AppSupport>();
        return builder;
    }

    /// <summary>
    /// Registers <see cref="IAppStore"/> for cross-platform store version lookups and deep links.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure">Optional inline configuration of <see cref="AppStoreOptions"/>.</param>
    public static MauiAppBuilder AddAppStore(
        this MauiAppBuilder builder,
        Action<AppStoreOptions>? configure = null
    )
    {
        if (configure != null)
            builder.Services.Configure(configure);

        builder.EnsureEssentials();
        builder.Services.TryAddSingleton<IAppStore, AppStore>();
        return builder;
    }

    /// <summary>
    /// Convenience overload for <see cref="AddAppStore(MauiAppBuilder, Action{AppStoreOptions})"/>
    /// that accepts the most common store identifiers directly.
    /// </summary>
    public static MauiAppBuilder AddAppStore(
        this MauiAppBuilder builder,
        string? appleAppId = null,
        string? androidPackageName = null,
        string? windowsProductId = null,
        string? countryCode = null
    ) => builder.AddAppStore(opts =>
    {
        if (appleAppId != null) opts.AppleAppId = appleAppId;
        if (androidPackageName != null) opts.AndroidPackageName = androidPackageName;
        if (windowsProductId != null) opts.WindowsProductId = windowsProductId;
        if (countryCode != null) opts.CountryCode = countryCode;
    });


    // The macOS (AppKit) head resolves the platform-neutral Microsoft.Maui.Essentials asset, whose members
    // all throw NotImplementedInReferenceAssembly. AddMacOSEssentials swaps the AppKit implementations in
    // behind the static Essentials APIs, so it has to have run before IAppSupport/IAppStore are resolved.
    // It's TryAdd-based, so an app that already called it isn't affected.
    static void EnsureEssentials(this MauiAppBuilder builder)
    {
#if MACOS
        builder.AddMacOSEssentials();
#endif
    }
}
