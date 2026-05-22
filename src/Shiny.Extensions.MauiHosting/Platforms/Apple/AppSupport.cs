using Foundation;
using Microsoft.Maui.Devices;

namespace Shiny.Impl;

public sealed partial class AppSupport
{
    // Constant isn't exposed by the .NET iOS binding, so we build the NSString manually.
    static readonly NSString SystemTimeZoneDidChangeNotification = new("NSSystemTimeZoneDidChangeNotification");

    NSObject? localeObserver;
    NSObject? timeZoneObserver;

    void StartCulture()
        => this.localeObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            NSLocale.CurrentLocaleDidChangeNotification,
            _ => this.RefreshCulture()
        );

    void StopCulture()
    {
        if (this.localeObserver == null)
            return;

        NSNotificationCenter.DefaultCenter.RemoveObserver(this.localeObserver);
        this.localeObserver.Dispose();
        this.localeObserver = null;
    }

    void StartTimeZone()
        => this.timeZoneObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            SystemTimeZoneDidChangeNotification,
            _ => this.RefreshTimeZone()
        );

    void StopTimeZone()
    {
        if (this.timeZoneObserver == null)
            return;

        NSNotificationCenter.DefaultCenter.RemoveObserver(this.timeZoneObserver);
        this.timeZoneObserver.Dispose();
        this.timeZoneObserver = null;
    }

    // iOS 16+ supports programmatic rotation via UIWindowScene.RequestGeometryUpdate. On iOS 15
    // and earlier there is no supported public API. macCatalyst windows don't rotate so it's a no-op.
    // Even on iOS 16+, the active view controller must permit the requested mask via its
    // supportedInterfaceOrientations override or the request is silently dropped.
    Task<bool> ApplyOrientation(DisplayOrientation orientation)
    {
#if IOS
        if (!OperatingSystem.IsIOSVersionAtLeast(16))
            return Task.FromResult(false);

        var scene = UIKit.UIApplication.SharedApplication.ConnectedScenes
            .ToArray()
            .OfType<UIKit.UIWindowScene>()
            .FirstOrDefault();

        if (scene == null)
            return Task.FromResult(false);

        var mask = orientation switch
        {
            DisplayOrientation.Portrait => UIKit.UIInterfaceOrientationMask.Portrait,
            DisplayOrientation.Landscape => UIKit.UIInterfaceOrientationMask.Landscape,
            _ => UIKit.UIInterfaceOrientationMask.All
        };

        var tcs = new TaskCompletionSource<bool>();
        var preferences = new UIKit.UIWindowSceneGeometryPreferencesIOS(mask);
        // UIKit threading rules require this call on the main thread.
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            scene.RequestGeometryUpdate(preferences, error => tcs.TrySetResult(error == null))
        );
        return tcs.Task;
#else
        return Task.FromResult(false);
#endif
    }
}
