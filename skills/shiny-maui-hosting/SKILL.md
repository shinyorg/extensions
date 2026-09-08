---
name: shiny-maui-hosting
description: Generate and configure Shiny MAUI Hosting for .NET - modular MAUI app configuration with IMauiModule, static ShinyHost.Services access, IAppSupport (device info + orientation/culture/timezone change events + programmatic orientation lock), IAppStore (cross-platform store version lookups and deep links for the Apple/Mac App Store, Google Play, Microsoft Store and Linux Flatpak/Snap), and IStartupService (install the app into the desktop OS launch-at-login list on Windows, macOS, and Linux). Covers the dotnet/maui-labs desktop backends - macOS AppKit (net10.0-macos) and Linux GTK4 via the companion Shiny.Extensions.MauiHosting.Linux package
auto_invoke: true
triggers:
  - IMauiModule
  - Shiny.Extensions.MauiHosting
  - ShinyHost.Services
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
  - IStartupService
  - AddStartupService
  - StartupServiceOptions
  - StartupServiceState
  - run at startup
  - launch at login
  - login item
  - autostart
  - AppKit
  - net10.0-macos
  - UseMauiAppMacOS
  - AddMacOSEssentials
  - Shiny.Extensions.MauiHosting.Linux
  - AddLinuxAppSupport
  - AddLinuxAppStore
  - UseMauiAppLinuxGtk4
  - LinuxAppId
  - GTK4
  - flatpak
  - snap
  - appstream
  - maui-labs
---

# Shiny MAUI Hosting Skill

You are an expert in Shiny Extensions MAUI Hosting, a .NET library providing modular MAUI app configuration via `IMauiModule`, a static service provider accessor, an `IAppSupport` service for device info and orientation/culture/timezone change detection, an `IAppStore` service for cross-platform store info and deep links, and an `IStartupService` for desktop launch-at-login registration.

Platform lifecycle hooks (`IIosLifecycle.*`, `IAndroidLifecycle.*`, `IMacLifecycle.*`) are wired automatically by `UseShiny()` from `Shiny.Hosting.Maui` — they are not handled by this library.

## When to Use This Skill

Invoke this skill when the user wants to:
- Create MAUI hosting modules with `IMauiModule`
- Access the service provider via `ShinyHost.Services`
- React to orientation, culture, or time-zone changes via `IAppSupport`
- Programmatically lock or reset device orientation
- Check store version / deep-link to store / launch a review page via `IAppStore`
- Install or remove the app from the desktop OS startup (launch at login) list via `IStartupService`

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
    .AddStartupService()                          // IStartupService + IOptions<StartupServiceOptions>
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
- **`Use(IPlatformApplication app)`** — post-build initialization. `ShinyHost.Services` is available here. **Do NOT block** — runs on the main thread.

```csharp
public class AnalyticsModule : IMauiModule
{
    public void Add(MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IAnalytics, AppCenterAnalytics>();
    }

    public void Use(IPlatformApplication app)
    {
        var analytics = ShinyHost.Services.GetRequiredService<IAnalytics>();
        analytics.TrackEvent("AppStarted");
    }
}
```

## Static ShinyHost Access

After initialization, `ShinyHost.Services` provides access to the service provider from anywhere:

```csharp
var service = ShinyHost.Services.GetRequiredService<IMyService>();
```

:::caution
`ShinyHost.Services` throws `InvalidOperationException` if accessed before initialization.
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

| Capability | iOS / macCatalyst / macOS | Android | Windows | Bare TFM |
|------------|---------------------------|---------|---------|----------|
| Orientation | `DeviceDisplay.MainDisplayInfoChanged` (MAUI) | `DeviceDisplay.MainDisplayInfoChanged` (MAUI) | `DeviceDisplay.MainDisplayInfoChanged` (MAUI) | 2s poll |
| Culture | `NSLocale.CurrentLocaleDidChangeNotification` | `BroadcastReceiver` on `Intent.ActionLocaleChanged` | `SystemEvents.UserPreferenceChanged` (Locale category) | 30s poll |
| Time zone | `NSSystemTimeZoneDidChangeNotification` | `BroadcastReceiver` on `Intent.ActionTimezoneChanged` | `SystemEvents.TimeChanged` | 30s poll |

The Linux GTK4 head uses `LinuxAppSupport` from `Shiny.Extensions.MauiHosting.Linux` instead — it watches
`/etc/localtime` via `FileSystemWatcher` for time-zone changes and polls for culture and orientation.

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
| macCatalyst / macOS (AppKit) / Linux (GTK4) | Not supported (desktop windows don't rotate) | Returns `false` |
| Windows | `DisplayInformation.AutoRotationPreferences` | `None` restores system default |

## IAppStore

`IAppStore` looks up the latest published version from the relevant platform store, exposes deep links, and launches the review page.

```csharp
public interface IAppStore
{
    Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default);
    Task<bool> OpenStore();
    Task<bool> OpenReviewPage();
    Task<bool> RequestReview();     // native in-app prompt where the OS has one
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
    public string? AppleAppId { get; set; }         // numeric App Store ID (iOS + Mac App Store deep links)
    public string? AppleBundleId { get; set; }      // defaults to AppInfo.PackageName
    public string? AndroidPackageName { get; set; } // defaults to AppInfo.PackageName
    public string? WindowsProductId { get; set; }   // required on Windows
    public string? LinuxAppId { get; set; }         // AppStream / Flatpak ID; auto-detected inside a Flatpak or Snap
    public string CountryCode { get; set; } = "us";
}
```

### Lookup behaviour

| Platform | API | Fields populated |
|----------|-----|------------------|
| iOS / macCatalyst | iTunes Search API (`itunes.apple.com/lookup?bundleId=…`) | All fields — version, release notes, ratings, release date, min OS. Auto-caches `trackId` back into `AppleAppId` for subsequent deep links |
| macOS (AppKit) | Same, plus `&entity=macSoftware` so iTunes answers with the Mac app rather than an iOS app sharing the bundle ID | Same as iOS |
| Linux (`Shiny.Extensions.MauiHosting.Linux`) | `flatpak info` for the installed version + origin, then `flatpak remote-info <origin> <id>`; or `snap list` + `snap info` for the tracked channel | Version, `NeedsUpdate`, store URL, `ReleasedAt`, and the Flatpak commit subject as `ReleaseNotes` |
| Android | Play Store HTML scrape (`play.google.com/store/apps/details?id=…`) with two `GeneratedRegex` strategies (JSON-LD `softwareVersion` and legacy `[[["x.y.z"]]]` AF_initDataCallback) | Version + `NeedsUpdate` only — Play HTML doesn't reliably expose other fields |
| Windows | Microsoft Store DisplayCatalog (`displaycatalog.mp.microsoft.com/v7.0/products?bigIds=…`) | Version, release notes (from `ProductDescription`), `ReleasedAt` where available |
| Other TFMs | Not supported | Returns `null` |

### Deep links

| Platform | OpenStore | OpenReviewPage |
|----------|-----------|----------------|
| iOS / macCatalyst | `itms-apps://itunes.apple.com/app/id{AppleAppId}` | `itms-apps://…/app/id{AppleAppId}?action=write-review` |
| macOS (AppKit) | `macappstore://apps.apple.com/app/id{AppleAppId}` | `macappstore://…/app/id{AppleAppId}?action=write-review` |
| Linux | `appstream://{LinuxAppId}` via `xdg-open` (GNOME Software / Plasma Discover / Snap Store) | Same as OpenStore — software centres show reviews on the app page |
| Android | `market://details?id={packageName}` | Same as OpenStore (Play Store has no separate review URL) |
| Windows | `ms-windows-store://pdp/?ProductId={WindowsProductId}` | `ms-windows-store://review/?ProductId={WindowsProductId}` |

`RequestReview` shows the OS's own in-app prompt: `StoreKit.AppStore.RequestReview` (a `UIWindowScene` on
iOS / Mac Catalyst 16+, the key window's `NSViewController` on macOS 14+) and
`StoreContext.RequestRateAndReviewAppAsync` on Windows. Android and Linux have no dependency-free in-app
prompt, so both fall back to `OpenReviewPage`.

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

## IStartupService

`IStartupService` installs the running app into the desktop operating system's startup ("launch at login") list. It is safe to call from cross-platform code — mobile reports `NotSupported` rather than throwing.

```csharp
public interface IStartupService
{
    bool IsSupported { get; }
    Task<StartupServiceState> GetState(CancellationToken cancellationToken = default);
    Task<StartupServiceState> Register(CancellationToken cancellationToken = default);
    Task<StartupServiceState> Unregister(CancellationToken cancellationToken = default);
    Task<bool> OpenSettings();          // OS startup-apps / login-items UI
}

public enum StartupServiceState
{
    NotSupported,
    NotRegistered,
    Enabled,
    DisabledByUser,      // registered, but switched off in Task Manager / Login Items / the .desktop file
    DisabledByPolicy,    // group policy / MDM — the app cannot override this
    RequiresApproval     // macOS only — submitted, waiting for the user to approve in System Settings
}

public class StartupServiceOptions
{
    public string? Identifier { get; set; }      // Windows Run value name / Linux .desktop file name; defaults to the entry assembly name
    public string? DisplayName { get; set; }     // Linux desktop entry Name; defaults to Identifier
    public string? ExecutablePath { get; set; }  // defaults to Environment.ProcessPath
    public IList<string> Arguments { get; set; } // Windows + Linux only
}
```

### Platform behaviour

| Platform | Mechanism | Notes |
|----------|-----------|-------|
| Windows (unpackaged, `WindowsPackageType=None`) | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Honours `ExecutablePath`/`Arguments`. `StartupApproved\Run` is read so a user switching the entry off in Task Manager surfaces as `DisabledByUser`. `OpenSettings` launches `ms-settings:startupapps` |
| Windows (MSIX packaged) | Not supported | MSIX virtualizes `HKCU` writes into a per-package hive, so a `Run` entry never reaches the shell. Packaged apps need a `windows.startupTask` manifest declaration driven through WinRT, which this package doesn't implement. `IsSupported` is false |
| macOS 13+ (Mac Catalyst and AppKit / `net10.0-macos`) | `SMAppService.MainApp` | Registers the running app bundle — `Identifier`, `ExecutablePath` and `Arguments` are all ignored. The first `Register` commonly returns `RequiresApproval` until the user approves it under System Settings > General > Login Items (`OpenSettings` opens exactly that pane). Uses `DispatchQueue.MainQueue` rather than MAUI's `MainThread`, so it works on the AppKit head |
| Linux (bare `net10.0` build) | `~/.config/autostart/{Identifier}.desktop` (honours `XDG_CONFIG_HOME`) | Honours `ExecutablePath`/`Arguments`. `Hidden=true` or `X-GNOME-Autostart-enabled=false` surfaces as `DisabledByUser`. `OpenSettings` returns false — there is no cross-desktop settings page |
| iOS / Android / macOS 12 and earlier | Not supported | `IsSupported` is false; every call returns `NotSupported` |

### Registering on macOS (AppKit)

`Shiny.Extensions.MauiHosting` multi-targets `net10.0-macos`. A plain AppKit app with no `MauiAppBuilder`
can register against the service collection instead:

```csharp
services.AddStartupService(opts => opts.Identifier = "MyApp");
```

Both overloads live in `Shiny.StartupServiceExtensions` — the `MauiAppBuilder` one just forwards to the
`IServiceCollection` one. `IStartupService` never touches MAUI Essentials, so it needs no extra setup on
the AppKit head.

### Usage

```csharp
public class StartupToggleViewModel(IStartupService startup)
{
    public bool CanToggle => startup.IsSupported;

    public async Task<StartupServiceState> Load() => await startup.GetState();

    public async Task<StartupServiceState> Set(bool runAtLogin)
    {
        var state = runAtLogin
            ? await startup.Register()
            : await startup.Unregister();

        // Not failures — the OS is telling you the user has to finish the job.
        if (state is StartupServiceState.RequiresApproval or StartupServiceState.DisabledByUser)
            await startup.OpenSettings();

        return state;
    }
}
```

Starting minimized is the app's job — register a marker argument and check it at launch:

```csharp
builder.AddStartupService(opts => opts.Arguments.Add("--autostart"));

var launchedAtLogin = Environment.GetCommandLineArgs().Contains("--autostart");
```

:::caution
`Register` returns the state the OS settled on, which is often not `Enabled`. Always bind your toggle to the returned state (or a fresh `GetState`) instead of assuming success — the user can switch the entry off outside your app at any time.
:::

`Register`/`Unregister` throw `InvalidOperationException` when the OS rejects the change outright (for example, a macOS login item that can't be submitted, or an HKCU key that can't be opened). `NotSupported`, `DisabledByUser`, `DisabledByPolicy` and `RequiresApproval` are returned states, not exceptions.

## Desktop backends (dotnet/maui-labs)

.NET MAUI ships no first-party macOS (AppKit) or Linux head. [dotnet/maui-labs](https://github.com/dotnet/maui-labs)
provides both as experimental `0.1.0-preview` packages, and this library supports them. On neither head does
MAUI Essentials have a real implementation — both resolve the platform-neutral `Microsoft.Maui.Essentials`
asset, where every static member throws `NotImplementedInReferenceAssembly` — so each backend needs its own
wiring. That difference is the whole reason Linux gets a separate package.

### macOS (AppKit) — `net10.0-macos`, in the base package

`Microsoft.Maui.Platforms.MacOS.Essentials` reflects AppKit implementations into the private backing fields
behind the static Essentials APIs (`AppInfo.Current`, `DeviceInfo.Current`, `Browser.Default`, …), so once
`AddMacOSEssentials()` has run the normal statics work. `Shiny.Extensions.MauiHosting` takes a hard
dependency on that package for the `-macos` TFM and calls it from `AddAppSupport()`/`AddAppStore()`, so app
code doesn't have to.

```csharp
using Microsoft.Maui.Platforms.MacOS.Hosting;
using Shiny;

var builder = MauiApp.CreateBuilder();
builder
    .UseMauiAppMacOS<App>()
    .AddInfrastructureModules(new MyMauiModule())
    .AddAppSupport()                                  // calls AddMacOSEssentials() for you
    .AddAppStore(opts => opts.AppleAppId = "1234567890")
    .AddStartupService();

return builder.Build();
```

The entry point is hand-written — there is no MAUI single-project generated `Main` on `-macos`:

```csharp
// Main.cs
public static class MainClass
{
    static void Main(string[] args)
    {
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new MauiMacOSApp();
        NSApplication.Main(args);
    }
}

// MauiMacOSApp.cs
[Register("MauiMacOSApp")]
public class MauiMacOSApp : MacOSMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
```

`MacOSMauiApplication` sets `IPlatformApplication.Current` before building the MAUI app, so `ShinyHost`
and `IMauiModule.Use` work exactly as on the other heads.

### Linux (GTK4) — `Shiny.Extensions.MauiHosting.Linux`

There is no `-linux` TFM, so a GTK4 head is a plain `net10.0` project — the same asset the base package
serves Linux from. `AddLinuxGtk4Essentials()` only redirects five of the static Essentials APIs
(`Preferences`, `FilePicker`, `SecureStorage`, `Clipboard`, `MediaPicker`), so `AppInfo.Version`,
`DeviceInfo.Model`, `Browser.OpenAsync` and friends still throw. The Linux package's implementations
therefore resolve the Essentials **interfaces** out of the container, which is where the GTK4 backend
registers them.

```csharp
using Microsoft.Maui.Platforms.Linux.Gtk4.Hosting;
using Shiny;

var builder = MauiApp.CreateBuilder();
builder
    .UseMauiAppLinuxGtk4<App>()
    .AddInfrastructureModules(new MyMauiModule())
    .AddLinuxAppSupport()                         // IAppSupport over the GTK4 Essentials services
    .AddLinuxAppStore("org.example.MyApp")        // IAppStore over Flatpak / Snap
    .AddStartupService();                         // XDG autostart, from the base package

return builder.Build();

// Program.cs
public class Program : GtkMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    public static void Main(string[] args) => new Program().Run(args);
}
```

| Capability | Linux behaviour |
|------------|-----------------|
| Device info / browser / map | `IAppInfo`, `IDeviceInfo`, `IBrowser`, `IMap` from `AddLinuxGtk4Essentials()` (browser and map go through `xdg-open`) |
| Time zone changes | `FileSystemWatcher` on `/etc/localtime`, which `systemd-timedated` replaces on a zone change; falls back to a 30s poll if the watch can't be created |
| Culture changes | 30s poll. A Linux locale switch only takes effect on the next login, so a running process never sees one from the OS |
| Orientation | Read from the GDK monitor geometry. `SetOrientation`/`ResetOrientation` return false |
| App store | `flatpak`/`snap` CLI, hopping the sandbox with `flatpak-spawn --host` when the app is itself a Flatpak |
| Startup | `IStartupService` from the base package — no Linux-specific registration needed |

:::caution
`AddLinuxAppSupport`/`AddLinuxAppStore` are deliberately named apart from `AddAppSupport`/`AddAppStore`.
Both packages put extensions on `MauiAppBuilder` in the `Shiny` namespace, and on a Linux head only the
`Linux` pair works — the base pair compiles but throws `NotImplementedInReferenceAssembly` at runtime.
Never suggest `AddAppSupport`/`AddAppStore` for a GTK4 head.
:::

Both packages are `0.1.0-preview` from dotnet/maui-labs and are outside the .NET MAUI support policy — say
so when suggesting them.

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

// Shiny.Extensions.MauiHosting.Linux
public static class LinuxHostingExtensions
{
    public static MauiAppBuilder AddLinuxAppSupport(this MauiAppBuilder builder);
    public static MauiAppBuilder AddLinuxAppStore(this MauiAppBuilder builder, Action<AppStoreOptions>? configure = null);
    public static MauiAppBuilder AddLinuxAppStore(this MauiAppBuilder builder, string linuxAppId);
}

public static class StartupServiceExtensions
{
    public static IServiceCollection AddStartupService(this IServiceCollection services, Action<StartupServiceOptions>? configure = null);
    public static MauiAppBuilder AddStartupService(this MauiAppBuilder builder, Action<StartupServiceOptions>? configure = null);
}

public class ShinyHost : IMauiInitializeService
{
    public static IServiceProvider Services { get; }
}
```

## Code Generation Instructions

- One module per concern (similar to web modules)
- Keep `Add()` for service registration and `Use()` for post-build initialization
- Do NOT block in `Use()` — it runs on the main thread during app startup
- Use `ShinyHost.Services` to resolve services after the app is built
- Register platform lifecycle handlers against `IIosLifecycle.*` / `IAndroidLifecycle.*` / `IMacLifecycle.*` (Shiny.Core); `UseShiny()` dispatches them
- For each capability the app needs (AppSupport, AppStore), call the matching `Add*` extension — they don't auto-register
- Gate any "run at startup" UI on `IStartupService.IsSupported` so it doesn't render on mobile
- For `IAppStore` on Windows, always configure `WindowsProductId` — there's no auto-detect (the package family name from `AppInfo` is a different concept than the Store ProductId)
- On a macOS AppKit head (`net10.0-macos`) use `AddAppSupport`/`AddAppStore` as normal — they wire `AddMacOSEssentials()` themselves
- On a Linux GTK4 head use `AddLinuxAppSupport`/`AddLinuxAppStore` from `Shiny.Extensions.MauiHosting.Linux` — never the base `AddAppSupport`/`AddAppStore`
- Set `AppStoreOptions.LinuxAppId` whenever the app isn't guaranteed to run from a Flatpak or Snap sandbox — there's nothing to auto-detect from otherwise

## Best Practices

1. **One concern per module** — separate modules for analytics, networking, auth, etc.
2. **Never block in Use()** — if you need async work, use `Task.Run` or similar
3. **Use ShinyHost.Services sparingly** — prefer constructor injection; use `ShinyHost.Services` only where DI is unavailable
4. **Register lifecycle handlers via DI** — use `[Singleton]` attributes on platform lifecycle handler classes (Shiny.Core's `IIosLifecycle.*` / `IAndroidLifecycle.*` / `IMacLifecycle.*`)
5. **Detach event handlers** — `IAppSupport`'s native listeners auto-stop when the last subscriber detaches, so always unsubscribe on dispose/teardown to free the OS listener
6. **Cache `AppStoreResult`** — store lookups are network calls; don't call `GetCurrent` on every navigation
7. **Never cache `StartupServiceState`** — read it with `GetState` each time the UI shows; the user can change it in the OS while your app runs
8. **Set `StartupServiceOptions.Identifier` explicitly** for shipping apps — the default (entry assembly name) changes if the assembly is ever renamed, orphaning the existing startup entry
