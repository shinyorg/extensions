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
| `StoreKeys.Default` ("settings") | iOS/macOS | NSUserDefaults |
| `StoreKeys.Default` ("settings") | Windows | ApplicationData.LocalSettings |
| `StoreKeys.Default` ("settings") | Blazor | localStorage |
| `StoreKeys.Secure` ("secure") | Android | EncryptedSharedPreferences |
| `StoreKeys.Secure` ("secure") | iOS/macOS | Keychain |

## Setup

```csharp
// Mobile/Desktop - registers platform-native stores + a hosted initializer for the static Shiny.Stores accessor
services.AddShinyStores();

// Blazor WebAssembly
services.AddShinyWebAssemblyStores();
```

## Static `Shiny.Stores` Accessor

The simplest way to read/write — backed by a hosted initializer that populates the static after the service provider is built.

```csharp
Shiny.Stores.Default.Set("theme", "dark");
var theme = Shiny.Stores.Default.Get<string>("theme");

Shiny.Stores.Secure.Set("token", "abc123");

// Arbitrary keyed stores
Shiny.Stores.Keyed("my-store").Set("k", "v");
```

For host-less scenarios (unit tests, console apps without `IHost`), call `Shiny.Stores.Initialize(serviceProvider)` after `BuildServiceProvider()`.

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
3. **Don't initialize manually in production** — `AddShinyStores()` registers a hosted initializer; only call `Shiny.Stores.Initialize(...)` in tests
