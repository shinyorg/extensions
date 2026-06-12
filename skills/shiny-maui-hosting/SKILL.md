---
name: shiny-maui-hosting
description: Generate and configure Shiny MAUI Hosting for .NET - modular MAUI app configuration with IMauiModule, static Host.Services access, IAppSupport (device info + orientation/culture/timezone change events + programmatic orientation lock), and IAppStore (cross-platform store version lookups and deep links for Apple, Google, Microsoft stores)
auto_invoke: true
triggers:
  - IMauiModule
  - Shiny.Extensions.MauiHosting
  - Host.Services
  - IAppSupport
  - IAppStore
  - AppStoreOptions
  - AppStoreResult
  - AddAppSupport
  - AddAppStore
  - AddInfrastructureModules
  - OrientationChanged
  - CultureChanged
  - TimeZoneChanged
  - SetOrientation
  - ResetOrientation
---

# Shiny MAUI Hosting Skill

You are an expert in Shiny Extensions MAUI Hosting, a .NET library providing modular MAUI app configuration via `IMauiModule`, a static service provider accessor, an `IAppSupport` service for device info and orientation/culture/timezone change detection, and an `IAppStore` service for cross-platform store info and deep links.

Platform lifecycle hooks (`IIosLifecycle.*`, `IAndroidLifecycle.*`, `IMacLifecycle.*`) are wired automatically by `UseShiny()` from `Shiny.Hosting.Maui` — they are not handled by this library.

## When to Use This Skill

Invoke this skill when the user wants to:
- Create MAUI hosting modules with `IMauiModule`
- Access the service provider via `Host.Services`
- React to orientation, culture, or time-zone changes via `IAppSupport`
- Programmatically lock or reset device orientation
- Check store version / deep-link to store / launch a review page via `IAppStore`

## Library Overview

**Documentation**: https://shinylib.net/mauihost/
**Repository**: https://github.com/shinyorg/extensions
**Package**: `Shiny.Extensions.MauiHosting`
**Namespace**: `Shiny`

## Registration

Starting in v4, each capability ships as its own extension method. `AddInfrastructureModules` only wires modules — opt into the rest:

```csharp
using Shiny;

var builder = MauiApp.CreateBuilder();
builder
    .UseMauiApp<App>()
    .AddInfrastructureModules(new MyModule(), new AnotherModule())
    .AddAppSupport()                              // IAppSupport
    .AddAppStore(opts =>                          // IAppStore + IOptions<AppStoreOptions>
    {
        opts.AppleAppId = "1234567890";
        opts.WindowsProductId = "9NBLGGH4NNS1";
        opts.CountryCode = "us";
    });

return builder.Build();
```

Each extension is idempotent (uses `TryAddSingleton` / `HasImplementation` guards) so it's safe to call from libraries.

`AddAppStore` has a convenience overload:
```csharp
builder.AddAppStore(appleAppId: "1234567890", windowsProductId: "9NBLGGH4NNS1");
```

## IMauiModule Interface

```csharp
public interface IMauiModule
{
    void Add(MauiAppBuilder builder);           // Register services
    void Use(IPlatformApplication app);         // Post-build initialization (do NOT block)
}
```

Each module implements two methods:
- **`Add(MauiAppBuilder builder)`** — register services, configure the builder. Runs before the app is built.
- **`Use(IPlatformApplication app)`** — post-build initialization. `Host.Services` is available here. **Do NOT block** — runs on the main thread.

```csharp
public class AnalyticsModule : IMauiModule
{
    public void Add(MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IAnalytics, AppCenterAnalytics>();
    }

    public void Use(IPlatformApplication app)
    {
        var analytics = Host.Services.GetRequiredService<IAnalytics>();
        analytics.TrackEvent("AppStarted");
    }
}
```

## Static Host Access

After initialization, `Host.Services` provides access to the service provider from anywhere:

```csharp
var service = Host.Services.GetRequiredService<IMyService>();
```

:::caution
`Host.Services` throws `InvalidOperationException` if accessed before initialization.
:::

## IAppSupport

`IAppSupport` exposes device info, browser/map launch, programmatic orientation lock, and change-detection events for orientation, culture, and time zone.

```csharp
public interface IAppSupport
{
    Version AppVersion { get; }
    string DeviceManufacturer { get; }
    string DeviceModel { get; }
    Version? PlatformVersion { get; }
    string Platform { get; }            // DeviceInfo.Platform.ToString() — "Android", "iOS", "WinUI", "macOS"
    DeviceIdiom DeviceIdiom { get; }    // DeviceInfo.Idiom — Phone / Tablet / Desktop / TV / Watch

    DisplayOrientation CurrentOrientation { get; }
    event EventHandler<DisplayOrientation>? OrientationChanged;

    CultureInfo CurrentCulture { get; }
    event EventHandler<CultureInfo>? CultureChanged;

    TimeZoneInfo CurrentTimeZone { get; }
    event EventHandler<TimeZoneInfo>? TimeZoneChanged;

    Task<bool> SetOrientation(DisplayOrientation orientation);
    Task<bool> ResetOrientation();

    Task<bool> OpenBrowser(string uri, /* … */);
    Task<bool> OpenMap(double latitude, double longitude, /* … */);
}
```

### Change-detection events

Each event has its own lazy subscription — the native listener spins up when the first handler attaches and tears down when the last detaches.

| Capability | iOS / macCatalyst | Android | Windows | Bare TFM |
|------------|-------------------|---------|---------|----------|
| Orientation | `DeviceDisplay.MainDisplayInfoChanged` (MAUI) | `DeviceDisplay.MainDisplayInfoChanged` (MAUI) | `DeviceDisplay.MainDisplayInfoChanged` (MAUI) | 2s poll |
| Culture | `NSLocale.CurrentLocaleDidChangeNotification` | `BroadcastReceiver` on `Intent.ActionLocaleChanged` | `SystemEvents.UserPreferenceChanged` (Locale category) | 30s poll |
| Time zone | `NSSystemTimeZoneDidChangeNotification` | `BroadcastReceiver` on `Intent.ActionTimezoneChanged` | `SystemEvents.TimeChanged` | 30s poll |

```csharp
public class SettingsViewModel(IAppSupport app)
{
    public void Init()
    {
        app.OrientationChanged += (s, o) => { /* new DisplayOrientation */ };
        app.CultureChanged += (s, c) => { /* new CultureInfo */ };
        app.TimeZoneChanged += (s, tz) => { /* new TimeZoneInfo */ };
    }
}
```

### Orientation lock

```csharp
await app.SetOrientation(DisplayOrientation.Landscape);
await app.ResetOrientation();   // restore system default
```

| Platform | Mechanism | Notes |
|----------|-----------|-------|
| Android | `Activity.RequestedOrientation` | Uses `SensorPortrait`/`SensorLandscape` so the device can still flip left↔right within the chosen orientation. Returns `false` if no current Activity |
| iOS 16+ | `UIWindowScene.RequestGeometryUpdate` | The active view controller must permit the requested mask via `supportedInterfaceOrientations` or the request is silently dropped |
| iOS 15 and earlier | Not supported | Returns `false` |
| macCatalyst | Not supported (windows don't rotate) | Returns `false` |
| Windows | `DisplayInformation.AutoRotationPreferences` | `None` restores system default |

## IAppStore

`IAppStore` looks up the latest published version from the relevant platform store, exposes deep links, and launches the review page.

```csharp
public interface IAppStore
{
    Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default);
    Task<bool> OpenStore();
    Task<bool> OpenReviewPage();
}

public record AppStoreResult(
    Version StoreVersion,
    Version CurrentVersion,
    bool NeedsUpdate,
    string StoreUrl,
    string? ReleaseNotes = null,
    DateTimeOffset? ReleasedAt = null,
    double? AverageRating = null,
    long? RatingCount = null,
    string? MinimumOsVersion = null
);

public class AppStoreOptions
{
    public string? AppleAppId { get; set; }         // numeric App Store ID (required for iOS deep links)
    public string? AppleBundleId { get; set; }      // defaults to AppInfo.PackageName
    public string? AndroidPackageName { get; set; } // defaults to AppInfo.PackageName
    public string? WindowsProductId { get; set; }   // required on Windows
    public string CountryCode { get; set; } = "us";
}
```

### Lookup behaviour

| Platform | API | Fields populated |
|----------|-----|------------------|
| iOS / macCatalyst | iTunes Search API (`itunes.apple.com/lookup?bundleId=…`) | All fields — version, release notes, ratings, release date, min OS. Auto-caches `trackId` back into `AppleAppId` for subsequent deep links |
| Android | Play Store HTML scrape (`play.google.com/store/apps/details?id=…`) with two `GeneratedRegex` strategies (JSON-LD `softwareVersion` and legacy `[[["x.y.z"]]]` AF_initDataCallback) | Version + `NeedsUpdate` only — Play HTML doesn't reliably expose other fields |
| Windows | Microsoft Store DisplayCatalog (`displaycatalog.mp.microsoft.com/v7.0/products?bigIds=…`) | Version, release notes (from `ProductDescription`), `ReleasedAt` where available |
| Other TFMs | Not supported | Returns `null` |

### Deep links

| Platform | OpenStore | OpenReviewPage |
|----------|-----------|----------------|
| iOS / macCatalyst | `itms-apps://itunes.apple.com/app/id{AppleAppId}` | `itms-apps://…/app/id{AppleAppId}?action=write-review` |
| Android | `market://details?id={packageName}` | Same as OpenStore (Play Store has no separate review URL) |
| Windows | `ms-windows-store://pdp/?ProductId={WindowsProductId}` | `ms-windows-store://review/?ProductId={WindowsProductId}` |

### Usage

```csharp
public class UpdateChecker(IAppStore store)
{
    public async Task CheckForUpdates(CancellationToken ct = default)
    {
        var result = await store.GetCurrent(ct);
        if (result?.NeedsUpdate == true)
        {
            // result.StoreVersion, result.CurrentVersion, result.ReleaseNotes
            await store.OpenStore();
        }
    }

    public Task PromptForReview() => store.OpenReviewPage();
}
```

:::caution
Android version detection relies on scraping the Play Store HTML. Google changes the page structure periodically — if `GetCurrent` returns `null` on Android even when the app exists, the regex likely needs updating.
:::

## Platform Lifecycle Hooks

Platform lifecycle is wired by `UseShiny()` in `Shiny.Hosting.Maui` — register handlers against the per-platform interfaces in `Shiny.Core` (`IIosLifecycle.*`, `IMacLifecycle.*`, `IAndroidLifecycle.*`). This library does not duplicate that surface.

## API Summary

```csharp
public static class MauiHostingExtensions
{
    public static MauiAppBuilder AddInfrastructureModules(this MauiAppBuilder builder, params IEnumerable<IMauiModule> modules);
    public static MauiAppBuilder AddAppSupport(this MauiAppBuilder builder);
    public static MauiAppBuilder AddAppStore(this MauiAppBuilder builder, Action<AppStoreOptions>? configure = null);
    public static MauiAppBuilder AddAppStore(this MauiAppBuilder builder, string? appleAppId = null, string? androidPackageName = null, string? windowsProductId = null, string? countryCode = null);
}

public class Host : IMauiInitializeService
{
    public static IServiceProvider Services { get; }
}
```

## Code Generation Instructions

- One module per concern (similar to web modules)
- Keep `Add()` for service registration and `Use()` for post-build initialization
- Do NOT block in `Use()` — it runs on the main thread during app startup
- Use `Host.Services` to resolve services after the app is built
- Register platform lifecycle handlers against `IIosLifecycle.*` / `IAndroidLifecycle.*` / `IMacLifecycle.*` (Shiny.Core); `UseShiny()` dispatches them
- For each capability the app needs (AppSupport, AppStore), call the matching `Add*` extension — they don't auto-register
- For `IAppStore` on Windows, always configure `WindowsProductId` — there's no auto-detect (the package family name from `AppInfo` is a different concept than the Store ProductId)

## Best Practices

1. **One concern per module** — separate modules for analytics, networking, auth, etc.
2. **Never block in Use()** — if you need async work, use `Task.Run` or similar
3. **Use Host.Services sparingly** — prefer constructor injection; use `Host.Services` only where DI is unavailable
4. **Register lifecycle handlers via DI** — use `[Singleton]` attributes on platform lifecycle handler classes (Shiny.Core's `IIosLifecycle.*` / `IAndroidLifecycle.*` / `IMacLifecycle.*`)
5. **Detach event handlers** — `IAppSupport`'s native listeners auto-stop when the last subscriber detaches, so always unsubscribe on dispose/teardown to free the OS listener
6. **Cache `AppStoreResult`** — store lookups are network calls; don't call `GetCurrent` on every navigation
