using System.Globalization;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
#if MACOS
using AppKit;
using CoreFoundation;
#else
using StoreKit;
using UIKit;
#endif

namespace Shiny.Impl;

public sealed partial class AppStore
{
#if MACOS
    // A macOS (AppKit) build is a Mac App Store product, so the lookup has to be scoped to macSoftware
    // or iTunes answers with the iOS app that shares the bundle identifier.
    const string LookupEntity = "&entity=macSoftware";

    // macappstore: opens the Mac App Store app; itms-apps: is the iOS/Mac Catalyst scheme.
    const string StoreUrlFormat = "macappstore://apps.apple.com/app/id{0}";
#else
    const string LookupEntity = "";
    const string StoreUrlFormat = "itms-apps://itunes.apple.com/app/id{0}";
#endif

    async Task<AppStoreResult?> LookupCurrent(CancellationToken cancellationToken)
    {
        // AppInfo.PackageName returns CFBundleIdentifier on Apple platforms.
        var bundleId = this.options.AppleBundleId ?? AppInfo.Current.PackageName;
        if (string.IsNullOrWhiteSpace(bundleId))
            return null;

        var url = $"https://itunes.apple.com/lookup?bundleId={Uri.EscapeDataString(bundleId)}&country={Uri.EscapeDataString(this.options.CountryCode)}{LookupEntity}";
        using var response = await this.http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return null;

        var entry = results[0];
        if (!TryGetVersion(entry, "version", out var storeVersion))
            return null;

        // Cache the discovered trackId so OpenStore/OpenReviewPage work without the caller wiring AppleAppId.
        if (entry.TryGetProperty("trackId", out var trackId) && trackId.ValueKind == JsonValueKind.Number)
            this.options.AppleAppId ??= trackId.GetInt64().ToString(CultureInfo.InvariantCulture);

        // AppInfo.Version is the running app's CFBundleShortVersionString — what users see in the store.
        var current = AppInfo.Current.Version;
        return new AppStoreResult(
            storeVersion,
            current,
            storeVersion > current,
            entry.TryGetProperty("trackViewUrl", out var trackUrl) ? trackUrl.GetString() ?? string.Empty : string.Empty,
            entry.TryGetProperty("releaseNotes", out var notes) ? notes.GetString() : null,
            entry.TryGetProperty("currentVersionReleaseDate", out var date) && date.TryGetDateTimeOffset(out var dto) ? dto : null,
            entry.TryGetProperty("averageUserRating", out var rating) && rating.ValueKind == JsonValueKind.Number ? rating.GetDouble() : null,
            entry.TryGetProperty("userRatingCount", out var count) && count.ValueKind == JsonValueKind.Number ? count.GetInt64() : null,
            entry.TryGetProperty("minimumOsVersion", out var minOs) ? minOs.GetString() : null
        );
    }

    Task<bool> OpenStoreCore()
    {
        // Need an actual App ID for the deep link — callers should configure it or call GetCurrent first.
        var appId = this.options.AppleAppId;
        if (string.IsNullOrWhiteSpace(appId))
            return Task.FromResult(false);

        return Launcher.Default.TryOpenAsync(new Uri(String.Format(CultureInfo.InvariantCulture, StoreUrlFormat, appId)));
    }

    Task<bool> OpenReviewPageCore()
    {
        var appId = this.options.AppleAppId;
        if (string.IsNullOrWhiteSpace(appId))
            return Task.FromResult(false);

        var url = String.Format(CultureInfo.InvariantCulture, StoreUrlFormat, appId) + "?action=write-review";
        return Launcher.Default.TryOpenAsync(new Uri(url));
    }

#if MACOS

    Task<bool> RequestReviewCore()
    {
        // AppStore.RequestReview(NSViewController) is the only supported in-app prompt on AppKit -
        // SKStoreReviewController.RequestReview() was deprecated in macOS 14. There is nothing to
        // anchor the prompt to before the first window exists, hence the false result.
        if (!OperatingSystem.IsMacOSVersionAtLeast(14))
            return Task.FromResult(false);

        var tcs = new TaskCompletionSource<bool>();

        // AppKit is main-thread only. DispatchQueue rather than MAUI's MainThread because MainThread
        // needs a MAUI Dispatcher, which isn't guaranteed to be running when this is called.
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            try
            {
                var app = NSApplication.SharedApplication;
                var controller =
                    app.KeyWindow?.ContentViewController ??
                    app.MainWindow?.ContentViewController;

                if (controller == null)
                {
                    tcs.SetResult(false);
                    return;
                }

                StoreKit.AppStore.RequestReview(controller);
                tcs.SetResult(true);
            }
            catch (Exception)
            {
                tcs.SetResult(false);
            }
        });
        return tcs.Task;
    }

#else

    Task<bool> RequestReviewCore()
    {
        var tcs = new TaskCompletionSource<bool>();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // RequestReview(UIWindowScene) is the iOS 14+ API; pick the foreground-active scene
                // so the prompt anchors to the visible window in multi-scene apps.
                var scene = UIApplication.SharedApplication.ConnectedScenes
                    .OfType<UIWindowScene>()
                    .FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive)
                    ?? UIApplication.SharedApplication.ConnectedScenes
                        .OfType<UIWindowScene>()
                        .FirstOrDefault();

                if (scene != null)
                {
                    // AppStore.RequestReview is the iOS/Mac Catalyst 16+ replacement; SKStoreReviewController
                    // was obsoleted on 18+. Fall back to the older API only where the new one isn't available.
                    if (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16))
                        StoreKit.AppStore.RequestReview(scene);
                    else
                        SKStoreReviewController.RequestReview(scene);

                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetResult(false);
                }
            }
            catch (Exception)
            {
                tcs.SetResult(false);
            }
        });
        return tcs.Task;
    }

#endif

    static bool TryGetVersion(JsonElement element, string propertyName, out Version version)
    {
        version = new Version(0, 0);
        if (!element.TryGetProperty(propertyName, out var prop))
            return false;

        var str = prop.GetString();
        return !string.IsNullOrWhiteSpace(str) && Version.TryParse(str, out version!);
    }
}
