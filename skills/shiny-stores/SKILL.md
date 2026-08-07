---
name: shiny-stores
description: Generate and configure Shiny Stores for .NET - cross-platform key/value stores with source-generated property binding for mobile, desktop, and Blazor WebAssembly
auto_invoke: true
triggers:
  - IKeyValueStore
  - AddShinyStores
  - AddShinyWebAssemblyStores
  - StoreKeys
  - Shiny.Stores
  - BindAttribute
  - Shiny.Extensions.Stores
  - Shiny.Extensions.Stores.Web
---

# Shiny Stores Skill

You are an expert in Shiny Extensions Stores, a .NET library providing cross-platform key/value store abstraction with source-generated property binding.

## When to Use This Skill

Invoke this skill when the user wants to:
- Use cross-platform key/value stores (settings, secure storage, memory)
- Persist properties to a backing store using the `[Bind]` source generator
- Use key/value stores in Blazor WebAssembly (localStorage)
- Create custom key/value store implementations

## Library Overview

**Documentation**: https://shinylib.net/extensions/stores/
**Repository**: https://github.com/shinyorg/Shiny.Extensions
**Packages**: `Shiny.Extensions.Stores`, `Shiny.Extensions.Stores.Web`

## Built-in Store Keys

Stores are registered as **keyed** singletons in DI using `StoreKeys` constants:

| Key | Platform | Implementation |
|-----|----------|---------------|
| `StoreKeys.Default` ("settings") | Android | SharedPreferences |
| `StoreKeys.Default` ("settings") | iOS / Mac Catalyst / macOS (`net10.0-macos`) | NSUserDefaults |
| `StoreKeys.Default` ("settings") | Windows (packaged) | ApplicationData.LocalSettings |
| `StoreKeys.Default` ("settings") | Linux / other desktop / unpackaged Windows | `FileKeyValueStore` (JSON file) |
| `StoreKeys.Default` ("settings") | Blazor | localStorage |
| `StoreKeys.Secure` ("secure") | Android | EncryptedSharedPreferences |
| `StoreKeys.Secure` ("secure") | iOS / Mac Catalyst / macOS (`net10.0-macos`) | Keychain |
| `StoreKeys.Secure` ("secure") | Windows | DPAPI (packaged: ApplicationData; unpackaged: over file) |
| `StoreKeys.Secure` ("secure") | Linux / other desktop | `FileKeyValueStore` (JSON file, **not encrypted**) |

`net10.0-macos` (plain macOS desktop apps) uses Foundation NSUserDefaults + Security.framework
Keychain — the same code as iOS/Mac Catalyst — so it gets **real** secure storage, not the plaintext
file fallback. Any target that resolves the base `net10.0` asset (Linux, unpackaged Windows, other
desktop) uses `FileKeyValueStore`, which persists to `{LocalApplicationData}/{EntryAssemblyName}` — so
settings survive restarts instead of living only in memory. Override the folder via
`Shiny.Stores.FileStoreDirectory` **before first access**. On these file fallbacks `Secure` is a plain
JSON file and is **not encrypted** (unpackaged Windows keeps DPAPI over the file); do not put
genuinely sensitive secrets there.

## Setup

```csharp
// Mobile / desktop — platform-native stores
services.AddShinyStores();

// Blazor WebAssembly — localStorage (still needs UseShinyStores after Build,
// because IJSRuntime is only available post-build)
services.AddShinyWebAssemblyStores();
```

On mobile/desktop you **do not need** a post-build `UseShinyStores()` call.
`Shiny.Stores.Default` / `Shiny.Stores.Secure` are self-bootstrapping: on first
access they lazily create the platform-native store (SharedPreferences /
NSUserDefaults / Keychain / DPAPI) or a persistent `FileKeyValueStore` on desktop
targets that resolve the base `net10.0` asset. `AddShinyStores()`
just registers those same instances into DI so keyed `IKeyValueStore` injections
share them.

For Blazor (where the store needs `IJSRuntime` from the built provider) call
`host.Services.UseShinyStores()` after `host.Build()` to snapshot the keyed
`IKeyValueStore` registrations into the static accessor.

## Static `Shiny.Stores` Accessor

The simplest way to read/write — self-bootstraps on first access. No
initialization required for mobile/desktop.

```csharp
Shiny.Stores.Default.Set("theme", "dark");
var theme = Shiny.Stores.Default.Get<string>("theme");

Shiny.Stores.Secure.Set("token", "abc123");

// Arbitrary keyed stores (must be registered with Stores.Register or via DI + UseShinyStores)
Shiny.Stores.Keyed("my-store").Set("k", "v");
```

### Overrides / Tests / Custom Keys

```csharp
// Swap any key for a test double or custom backend
Shiny.Stores.Register(StoreKeys.Default, new MemoryKeyValueStore());
Shiny.Stores.Register("redis", new RedisKeyValueStore(...));

// Or snapshot keyed IKeyValueStore registrations from a built provider:
serviceProvider.UseShinyStores();
// Equivalent low-level call:
Shiny.Stores.Initialize(serviceProvider);

// Reset between tests:
Shiny.Stores.Reset();
```

## DI-Style Access

```csharp
public class SettingsService(
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore settings,
    [FromKeyedServices(StoreKeys.Secure)] IKeyValueStore secure
)
{
    public void SaveTheme(string theme) => settings.Set("theme", theme);
    public string GetTheme() => settings.Get<string>("theme") ?? "light";
}
```

`AddShinyStores()` also registers the default store **unkeyed**, so a plain
`IKeyValueStore` parameter resolves to the same instance as
`[FromKeyedServices(StoreKeys.Default)]`.

### Third-party containers

Container adapters that predate .NET 8 keyed services — Prism's DryIoc container is the
common one, it still sits on DryIoc 5.x — silently **ignore** `[FromKeyedServices]` and
resolve the plain service type instead. The unkeyed registration above is what keeps the
default store working there. `StoreKeys.Secure` has no such fallback (registering two
different stores under one service type would be ambiguous), so when generating code for
an app on a non-Microsoft container, reach for the static accessor instead:

```csharp
public class SettingsService
{
    readonly IKeyValueStore secure = Shiny.Stores.Secure;   // or Shiny.Stores.Keyed("my-store")
}
```

A DryIoc `Error.UnableToFindCtorWithAllResolvableArgs` naming a type that injects a keyed
store is this problem — DryIoc's `ConstructorWithResolvableArguments` rule reports the
outermost type, not the dependency that actually failed.

## Source-Generated `[Bind]` Properties

The DI source generator (from `Shiny.Extensions.DependencyInjection`) recognizes `[Bind]` on partial properties and emits getter/setter bodies that round-trip through the static `Shiny.Stores` accessor.

```csharp
using Shiny;

[Singleton]
public partial class AppSettings
{
    [Bind]                                   // default store
    public partial string Theme { get; set; }

    [Bind("secure")]                         // secure store
    public partial string Token { get; set; }

    [Bind(Key = "ui_density")]               // override storage key
    public partial int Density { get; set; }
}
```

No `INotifyPropertyChanged`, no runtime reflection. Generated property bodies call `Shiny.Stores.Default/Secure/Keyed(...).Get<T>(...)` and `.Set(...)`.

## Store Extension Methods

```csharp
store.Get<T>(key, defaultValue);        // Get with default
store.GetRequired<T>(key);              // Throws if not found
store.SetOrRemove(key, value);          // Removes if value is null
store.SetDefault<T>(key, value);        // Only sets if key doesn't exist
store.IncrementValue(key);              // Thread-safe integer increment
```

## Code Generation Instructions

- Use `AddShinyStores()` for mobile/desktop, `AddShinyWebAssemblyStores()` for Blazor
- For persistent settings, prefer `[Singleton]` + `[Bind]` partial properties over manual `Set`/`Get` calls
- The class with `[Bind]` properties must be `partial`; properties must also be `partial`
- For sensitive data (tokens, credentials), pass `"secure"` to `[Bind("secure")]`
- Use `Shiny.Stores.Default`/`Secure`/`Keyed(...)` for direct ad-hoc access

## Best Practices

1. **Use `[Bind]` for settings classes** — eliminates boilerplate, no INPC needed, AOT-clean
2. **Target the secure store** — always use `[Bind("secure")]` for sensitive values
3. **Mobile/desktop needs no post-build call** — `Shiny.Stores` self-bootstraps on first access. `UseShinyStores()` is only needed for Blazor (because `IJSRuntime` requires the built provider) or when you've registered custom keyed `IKeyValueStore`s in DI that you want snapshotted into the static accessor
4. **Use `Stores.Register` in tests** — pair with `Stores.Reset()` between tests to swap in `MemoryKeyValueStore` or any custom double
