using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace Shiny;

public static class MauiExtensions
{
    /// <summary>
    ///  Adds the specified modules to the MauiAppBuilder. This will call the Add method on each module, allowing them to add services to the MauiAppBuilder. It will also add a singleton of IMauiInitializeService that will call the Use method on each module when the app is initialized. This allows you to do things like initialize Shiny or other services that need to be initialized at startup.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="modules"></param>
    /// <returns></returns>
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

        if (builder.Services.HasImplementation<Host>())
            return builder;
        
#if ANDROID || IOS || MACCATALYST
        builder.Services.AddSingleton<ILifecycleExecutor, LifecycleExecutor>();
#endif
        
        builder.Services.AddSingleton<IMauiInitializeService, Host>();
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
            // Shiny will supply push events & handle background url for http transfers
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
    ///  Adds the platform lifecycle hooks to the MauiAppBuilder. This will add the necessary services to the MauiAppBuilder to allow you to use the platform lifecycle hooks in your app
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static MauiAppBuilder AddPlatformLifecycleDependencyHooks(this MauiAppBuilder builder)
    {
        
        return builder;
    }
}