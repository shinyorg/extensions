---
name: shiny-blazor-hosting
description: Generate and configure Shiny Blazor Hosting for .NET - IAppSupport for Blazor WebAssembly providing app version, browser user-agent, screen/viewport dimensions, and live culture/time-zone change events via synchronous JS interop
auto_invoke: true
triggers:
  - Shiny.Extensions.BlazorHosting
  - IAppSupport
  - AddAppSupport
  - UserAgent
  - UserAgentVersion
  - BrowserWidth
  - BrowserHeight
  - ScreenWidth
  - ScreenHeight
  - CultureChanged
  - TimeZoneChanged
  - shiny-appsupport.js
---

# Shiny Blazor Hosting Skill

You are an expert in Shiny Extensions Blazor Hosting, a .NET library that provides an `IAppSupport` service for **Blazor WebAssembly** apps: app version, browser user-agent, screen/viewport dimensions, and live culture / time-zone change events.

It is the browser-side sibling of `Shiny.Extensions.MauiHosting`'s `IAppSupport`. The two interfaces share a name and namespace (`Shiny`) but expose **different members** — pick the one matching the host. This skill is for the Blazor WebAssembly variant.

## When to Use This Skill

Invoke this skill when the user wants to:
- Read app/browser/device info in a Blazor WebAssembly app (`AppVersion`, `UserAgent`, screen/viewport sizes)
- React to culture or time-zone changes in the browser via `IAppSupport`
- Wire up `Shiny.Extensions.BlazorHosting`

## Library Overview

**Documentation**: https://shinylib.net/blazorhost/
**Repository**: https://github.com/shinyorg/extensions
**Package**: `Shiny.Extensions.BlazorHosting`
**Namespace**: `Shiny`
**Host**: Blazor WebAssembly only (uses `IJSInProcessRuntime` for synchronous browser reads)

## IAppSupport Interface

```csharp
public interface IAppSupport
{
    Version AppVersion { get; }          // supplied at registration from the head assembly — no reflection
    string? UserAgent { get; }           // navigator.userAgent
    Version? UserAgentVersion { get; }   // best-effort browser version parsed from the UA string (null if unparseable)

    int ScreenWidth { get; }             // window.screen.width
    int ScreenHeight { get; }            // window.screen.height
    int BrowserWidth { get; }            // window.innerWidth  (viewport — read live, changes on resize)
    int BrowserHeight { get; }           // window.innerHeight (viewport — read live, changes on resize)

    CultureInfo CurrentCulture { get; }
    event EventHandler<CultureInfo>? CultureChanged;

    TimeZoneInfo CurrentTimeZone { get; }
    event EventHandler<TimeZoneInfo>? TimeZoneChanged;
}
```

| Member | Source | Notes |
|--------|--------|-------|
| `AppVersion` | Registration argument | Pass a compile-time constant (`ThisAssembly.AssemblyVersion`) — never reflected off the entry assembly |
| `UserAgent` | `navigator.userAgent` | Read once and cached (fixed for the page's lifetime) |
| `UserAgentVersion` | Parsed from `UserAgent` | Best-effort browser version, tokens tried Edge → Opera → Firefox → Chrome → Safari; `null` if none parse |
| `ScreenWidth` / `ScreenHeight` | `window.screen.*` | Physical screen size |
| `BrowserWidth` / `BrowserHeight` | `window.innerWidth/innerHeight` | Viewport size — read live on each access, so reflects resizes |
| `CurrentCulture` + `CultureChanged` | `CultureInfo.CurrentCulture` | 30-second poller raises the event on change |
| `CurrentTimeZone` + `TimeZoneChanged` | `TimeZoneInfo.Local` | 30-second poller raises the event on change |

The change-detection events use lazy subscription: the poll timer starts when the first handler attaches and stops when the last detaches.

## Setup

1. Install the NuGet package `Shiny.Extensions.BlazorHosting`.

2. Reference the bundled JS in `wwwroot/index.html`, **before** `blazor.webassembly.js`. It attaches `window.shinyAppSupport` so reads can run synchronously through `IJSInProcessRuntime`:
   ```html
   <script src="_content/Shiny.Extensions.BlazorHosting/shiny-appsupport.js"></script>
   ```

3. Register in `Program.cs`, passing the head app's version constant (no reflection):
   ```csharp
   using Shiny;

   var builder = WebAssemblyHostBuilder.CreateDefault(args);
   builder.Services.AddAppSupport(ThisAssembly.AssemblyVersion);   // string overload parses to Version
   // or: builder.Services.AddAppSupport(new Version(1, 2, 3));
   ```
   `AddAppSupport` is idempotent (`TryAddSingleton`), so it's safe to call from libraries.

## Usage

```csharp
@inject IAppSupport App

@code {
    protected override void OnInitialized()
    {
        var version = App.AppVersion;
        var browser = App.UserAgentVersion;        // e.g. 126.0.0.0, or null
        var (w, h) = (App.BrowserWidth, App.BrowserHeight);

        App.CultureChanged += (s, c) => InvokeAsync(StateHasChanged);
        App.TimeZoneChanged += (s, tz) => InvokeAsync(StateHasChanged);
    }
}
```

## API Summary

```csharp
public static class BlazorHostingExtensions
{
    public static IServiceCollection AddAppSupport(this IServiceCollection services, Version appVersion);
    public static IServiceCollection AddAppSupport(this IServiceCollection services, string appVersion); // Version.Parse
}
```

## Code Generation Instructions

- Always emit the `<script src="_content/Shiny.Extensions.BlazorHosting/shiny-appsupport.js">` tag in `index.html` before `blazor.webassembly.js`; the service throws on first JS call if it's missing.
- Pass `ThisAssembly.AssemblyVersion` (or another compile-time constant) to `AddAppSupport` — do not reflect the entry assembly version.
- This is WebAssembly-only. For Blazor Server (no `IJSInProcessRuntime`), the synchronous property reads won't work — don't suggest it there.
- For MAUI apps, use `Shiny.Extensions.MauiHosting`'s `IAppSupport` instead (different members: orientation, device manufacturer/model, browser/map launch).
- Detach `CultureChanged` / `TimeZoneChanged` handlers on `Dispose` so the poll timer stops.

## Best Practices

1. **Read viewport dimensions on demand** — `BrowserWidth`/`BrowserHeight` reflect the live window, so read them when needed rather than caching.
2. **Marshal event handlers to the UI** — `CultureChanged` / `TimeZoneChanged` may fire off the render context; wrap UI updates in `InvokeAsync(StateHasChanged)`.
3. **Treat `UserAgentVersion` as best-effort** — UA strings are unreliable; handle `null` and don't gate critical logic on an exact browser version.
