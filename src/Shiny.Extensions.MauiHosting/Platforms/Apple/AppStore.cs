using System.Globalization;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using StoreKit;
using UIKit;

namespace Shiny.Impl;

public sealed partial class AppStore
{
    async Task<AppStoreResult?> LookupCurrent(CancellationToken cancellationToken)
    {
        // AppInfo.PackageName returns CFBundleIdentifier on Apple platforms.
        var bundleId = this.options.AppleBundleId ?? AppInfo.Current.PackageName;
        if (string.IsNullOrWhiteSpace(bundleId))
            return null;

        var url = $"https://itunes.apple.com/lookup?bundleId={Uri.EscapeDataString(bundleId)}&country={Uri.EscapeDataString(this.options.CountryCode)}";
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

        return Launcher.Default.TryOpenAsync(new Uri($"itms-apps://itunes.apple.com/app/id{appId}"));
    }

    Task<bool> OpenReviewPageCore()
    {
        var appId = this.options.AppleAppId;
        if (string.IsNullOrWhiteSpace(appId))
            return Task.FromResult(false);

        return Launcher.Default.TryOpenAsync(new Uri($"itms-apps://itunes.apple.com/app/id{appId}?action=write-review"));
    }

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

    static bool TryGetVersion(JsonElement element, string propertyName, out Version version)
    {
        version = new Version(0, 0);
        if (!element.TryGetProperty(propertyName, out var prop))
            return false;

        var str = prop.GetString();
        return !string.IsNullOrWhiteSpace(str) && Version.TryParse(str, out version!);
    }
}
