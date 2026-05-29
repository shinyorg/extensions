using System.Text.RegularExpressions;
using Microsoft.Maui.ApplicationModel;
using Xamarin.Google.Android.Play.Core.Review;

namespace Shiny.Impl;

public sealed partial class AppStore
{
    // Two regex strategies because Google's HTML changes frequently. The JSON-LD pattern is more stable
    // but isn't always present; the [[["x.y.z"]]] AF_initDataCallback pattern is the legacy fallback.
    [GeneratedRegex("""\[\[\["(?<v>\d+(?:\.\d+){1,3})"\]\]""", RegexOptions.Singleline)]
    private static partial Regex GooglePlayVersionRegex();

    [GeneratedRegex(""""softwareVersion"\s*:\s*"(?<v>\d+(?:\.\d+){1,3})"""", RegexOptions.Singleline)]
    private static partial Regex GooglePlayJsonLdVersionRegex();

    async Task<AppStoreResult?> LookupCurrent(CancellationToken cancellationToken)
    {
        // AppInfo.PackageName == applicationId on Android.
        var pkg = this.options.AndroidPackageName ?? AppInfo.Current.PackageName;
        if (string.IsNullOrWhiteSpace(pkg))
            return null;

        var url = $"https://play.google.com/store/apps/details?id={Uri.EscapeDataString(pkg)}&hl=en&gl={Uri.EscapeDataString(this.options.CountryCode)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Google serves a different (often heavier) page to mobile user agents — pin a desktop UA for stable scraping.
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ShinyAppStore/1.0)");

        using var response = await this.http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var versionMatch = GooglePlayJsonLdVersionRegex().Match(html);
        if (!versionMatch.Success)
            versionMatch = GooglePlayVersionRegex().Match(html);

        if (!versionMatch.Success)
            return null;

        if (!Version.TryParse(versionMatch.Groups["v"].Value, out var storeVersion))
            return null;

        var current = AppInfo.Current.Version;
        return new AppStoreResult(
            storeVersion,
            current,
            storeVersion > current,
            $"https://play.google.com/store/apps/details?id={pkg}"
        );
    }

    Task<bool> OpenStoreCore()
    {
        var pkg = this.options.AndroidPackageName ?? AppInfo.Current.PackageName;
        return Launcher.Default.TryOpenAsync(new Uri($"market://details?id={pkg}"));
    }

    // Play Store opens the listing on the reviews tab when the user scrolls — there is no separate review URL.
    Task<bool> OpenReviewPageCore() => this.OpenStoreCore();

    async Task<bool> RequestReviewCore()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null)
            return false;

        try
        {
            var manager = ReviewManagerFactory.Create(activity);

            // Play Core surfaces results through GMS Task completion listeners rather than awaitables,
            // so bridge each flow to a TaskCompletionSource.
            var requestTcs = new TaskCompletionSource<Android.Gms.Tasks.Task>();
            manager.RequestReviewFlow().AddOnCompleteListener(new ReviewFlowListener(requestTcs));
            var requestFlow = await requestTcs.Task.ConfigureAwait(false);

            if (!requestFlow.IsSuccessful || requestFlow.Result is not ReviewInfo reviewInfo)
                return false;

            var launchTcs = new TaskCompletionSource<Android.Gms.Tasks.Task>();
            manager.LaunchReviewFlow(activity, reviewInfo).AddOnCompleteListener(new ReviewFlowListener(launchTcs));
            var launchFlow = await launchTcs.Task.ConfigureAwait(false);

            // The OS may suppress the actual sheet (quota); a successful flow is the best signal available.
            return launchFlow.IsSuccessful;
        }
        catch (Exception)
        {
            return false;
        }
    }

    sealed class ReviewFlowListener(TaskCompletionSource<Android.Gms.Tasks.Task> tcs)
        : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        public void OnComplete(Android.Gms.Tasks.Task task) => tcs.TrySetResult(task);
    }
}
