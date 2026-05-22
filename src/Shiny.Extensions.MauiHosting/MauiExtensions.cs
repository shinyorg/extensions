using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Shiny.Impl;

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
            Host.Modules.Add(module);
        }

        if (!builder.Services.HasImplementation<Host>())
            builder.Services.AddSingleton<IMauiInitializeService, Host>();

        return builder;
    }

    /// <summary>
    /// Wires platform lifecycle events through Shiny's ILifecycleExecutor so handlers
    /// (IOnApplicationCreated, IOnFinishedLaunching, IAppForeground, etc.) get invoked.
    /// </summary>
    public static MauiAppBuilder AddPlatformLifecycle(this MauiAppBuilder builder)
    {
#if ANDROID || IOS || MACCATALYST
        if (builder.Services.HasImplementation<LifecycleExecutor>())
            return builder;

        builder.Services.AddSingleton<ILifecycleExecutor, LifecycleExecutor>();
#endif

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnApplicationCreate(x => Host.Lifecycle.OnApplicationCreated(x))
                .OnCreate((activity, savedInstanceState) => Host.Lifecycle.OnActivityOnCreate(activity, savedInstanceState))
                .OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) => Host.Lifecycle.OnActivityRequestPermissionResult(activity, requestCode, permissions, grantResults))
                .OnActivityResult((activity, requestCode, result, intent) => Host.Lifecycle.OnActivityResult(activity, requestCode, result, intent))
                .OnNewIntent((activity, intent) => Host.Lifecycle.OnActivityNewIntent(activity, intent))
            );
#elif APPLE
            events.AddiOS(ios => ios
                .WillEnterForeground(_ => Host.Lifecycle.OnAppForeground())
                .DidEnterBackground(_ => Host.Lifecycle.OnAppBackground())
                .FinishedLaunching((_, del) =>
                {
                    Host.Lifecycle.OnFinishLaunching(del);
                    return true;
                })
                .ContinueUserActivity((_, activity, handler) => Host.Lifecycle.OnContinueUserActivity(activity))
            );
#elif WINDOWS
            // events.AddWindows(win => win
            //     .OnLaunching((app, args) => { })
            //     .OnClosed((app, args) => { })
            //     .OnVisibilityChanged((app, args) => { })
            // );
#endif
        });

        return builder;
    }

    /// <summary>
    /// Registers <see cref="IAppSupport"/> for device info, browser/map launch helpers,
    /// and orientation / culture / time-zone change notifications.
    /// </summary>
    public static MauiAppBuilder AddAppSupport(this MauiAppBuilder builder)
    {
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
}
