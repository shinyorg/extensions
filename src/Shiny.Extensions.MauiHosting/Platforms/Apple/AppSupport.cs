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
    // and earlier there is no supported public API. macCatalyst and macOS (AppKit) windows don't
    // rotate so it's a no-op there.
    //
    // Three things this has to get right, each of which it once got wrong:
    //
    // - RequestGeometryUpdate's handler is called on FAILURE ONLY. Completing the task from it meant a
    //   rotation that worked never returned. The result is instead whether the display ended up in
    //   the requested orientation, read once the rotation has had time - the request's own verdict is
    //   not reliable either: it is refused against a view controller's stale supported orientations
    //   even while the narrowed mask is turning the device.
    // - The scene is the app's own foreground one. ConnectedScenes also holds an external display's
    //   scene (AirPlay), and rotating that is not what anybody asked for.
    // - A request is a single rotation, not a lock. What holds the device is the app's allowed mask,
    //   which UIKit asks the app delegate for - so it is kept in IosOrientationLock.Supported, and the
    //   app's delegate returns it (see there). Every view controller is then told to re-read it.
    Task<bool> ApplyOrientation(DisplayOrientation orientation)
    {
#if IOS
        if (!OperatingSystem.IsIOSVersionAtLeast(16))
            return Task.FromResult(false);

        var mask = orientation switch
        {
            DisplayOrientation.Portrait => UIKit.UIInterfaceOrientationMask.Portrait,
            DisplayOrientation.Landscape => UIKit.UIInterfaceOrientationMask.Landscape,
            _ => UIKit.UIInterfaceOrientationMask.All
        };

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // UIKit threading rules require all of this on the main thread.
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            IosOrientationLock.Supported = mask;

            var scene = UIKit.UIApplication.SharedApplication.ConnectedScenes
                .ToArray()
                .OfType<UIKit.UIWindowScene>()
                .Where(x => x.Session.Role == UIKit.UIWindowSceneSessionRole.Application)
                .OrderByDescending(x => x.ActivationState == UIKit.UISceneActivationState.ForegroundActive)
                .FirstOrDefault();

            if (scene == null)
            {
                tcs.TrySetResult(false);
                return;
            }

            foreach (var window in scene.Windows)
                RefreshSupportedOrientations(window.RootViewController);

            // Clearing the lock turns nothing - the device stays where it is until it is rotated.
            if (mask == UIKit.UIInterfaceOrientationMask.All)
            {
                tcs.TrySetResult(true);
                return;
            }

            // On the next turn of the main loop: view controllers re-read their supported
            // orientations asynchronously after SetNeedsUpdateOfSupportedInterfaceOrientations, and a
            // request made in the same turn is refused against the old ones.
            CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                scene.RequestGeometryUpdate(new UIKit.UIWindowSceneGeometryPreferencesIOS(mask), _ => { });

                _ = Task.Delay(RotationSettle).ContinueWith(_ =>
                    tcs.TrySetResult(DeviceDisplay.Current.MainDisplayInfo.Orientation == orientation)
                );
            });
        });

        return tcs.Task;
#else
        return Task.FromResult(false);
#endif
    }

#if IOS
    /// <summary>How long a rotation is given before the result is read.</summary>
    static readonly TimeSpan RotationSettle = TimeSpan.FromMilliseconds(700);

    // Every controller, not only the root: under Shell (tab bar -> navigation -> page) the one UIKit
    // asks is a child, and one left with its old answer refuses the change.
    static void RefreshSupportedOrientations(UIKit.UIViewController? controller)
    {
        if (controller == null)
            return;

        controller.SetNeedsUpdateOfSupportedInterfaceOrientations();

        foreach (var child in controller.ChildViewControllers)
            RefreshSupportedOrientations(child);

        RefreshSupportedOrientations(controller.PresentedViewController);
    }
#endif
}
