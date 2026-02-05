# API Reference

## Installation

```bash
# Dependency Injection with source generators
dotnet add package Shiny.Extensions.DependencyInjection

# Cross-platform key/value stores (mobile/desktop)
dotnet add package Shiny.Extensions.Stores

# Blazor WebAssembly stores (localStorage/sessionStorage)
dotnet add package Shiny.Extensions.Stores.Web

# ASP.NET infrastructure modules
dotnet add package Shiny.Extensions.WebHosting
```

## NuGet Packages Reference

| Package | Description |
|---------|-------------|
| `Shiny.Extensions.DependencyInjection` | Attribute-driven DI registration with source generators |
| `Shiny.Extensions.Stores` | Cross-platform key/value store abstraction with platform implementations |
| `Shiny.Extensions.Stores.Web` | Blazor WebAssembly localStorage and sessionStorage implementations |
| `Shiny.Extensions.WebHosting` | ASP.NET modular infrastructure module system |

---

## Dependency Injection Module

**Namespace:** `Shiny.Extensions.DependencyInjection`

### ServiceAttribute

Base attribute for service registration. Applied to classes.

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ServiceAttribute(ServiceLifetime lifetime) : Attribute
{
    public bool AsSelf { get; set; }
    public Type? Type { get; set; }
    public string? KeyedName { get; set; }
    public string? Category { get; set; }
    public bool TryAdd { get; set; }
}
```

### Convenience Attributes

```csharp
public class SingletonAttribute() : ServiceAttribute(ServiceLifetime.Singleton);
public class ScopedAttribute() : ServiceAttribute(ServiceLifetime.Scoped);
public class TransientAttribute() : ServiceAttribute(ServiceLifetime.Transient);
```

### Attribute Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AsSelf` | `bool` | `false` | Register the class as itself rather than its implemented interfaces |
| `Type` | `Type?` | `null` | Register against a specific interface (for classes implementing multiple interfaces) |
| `KeyedName` | `string?` | `null` | Register as a keyed service with this name |
| `Category` | `string?` | `null` | Category for conditional registration via `AddShinyServiceRegistry("category")` |
| `TryAdd` | `bool` | `false` | Use TryAdd semantics (won't replace existing registrations) |

### IShinyStartupTask

```csharp
public interface IShinyStartupTask
{
    void Start();
}
```

### ServiceProviderExtensions

```csharp
public static class ServiceProviderExtensions
{
    // Register all source-generated services across all libraries
    public static IServiceCollection AddShinyServiceRegistry(
        this IServiceCollection services,
        params IEnumerable<string> categories);

    // Check if a service type is registered
    public static bool HasService<TService>(this IServiceCollection services);
    public static bool HasService(this IServiceCollection services, Type serviceType);

    // Check if an implementation type is registered
    public static bool HasImplementation<TImpl>(this IServiceCollection services);
    public static bool HasImplementation(this IServiceCollection services, Type implementationType);

    // Lazy service resolution
    public static Lazy<T> GetLazyService<T>(this IServiceProvider services, bool required = false);

    // Run all registered IShinyStartupTask instances
    public static void RunStartupTasks(this IServiceProvider services);
}
```

### ServiceCollectionExtensions

```csharp
public static class ServiceCollectionExtensions
{
    // Register singleton against all implemented interfaces (same instance for all)
    public static IServiceCollection AddSingletonAsImplementedInterfaces<TImpl>(
        this IServiceCollection services,
        string? keyName = null) where TImpl : class;

    // Register scoped against all implemented interfaces (same instance per scope)
    public static IServiceCollection AddScopedAsImplementedInterfaces<TImpl>(
        this IServiceCollection services,
        string? keyName = null) where TImpl : class;
}
```

### Source Generator Internals

**Namespace:** `Shiny.Extensions.DependencyInjection.Internals`

```csharp
public static class ServiceRegistry
{
    // Used by source generators to register callbacks
    public static void RegisterCallback(
        Action<IServiceCollection, IEnumerable<string>> callback);

    // Called by AddShinyServiceRegistry to execute all callbacks
    public static void RunCallbacks(
        IServiceCollection services,
        params IEnumerable<string> categories);
}
```

The source generator produces a `__ShinyServicesModule` class with a `[ModuleInitializer]` that calls `ServiceRegistry.RegisterCallback()` at assembly load time. When `AddShinyServiceRegistry()` is called, all registered callbacks execute to register services.

---

## Stores Module

**Namespace:** `Shiny.Extensions.Stores`

### IKeyValueStore

```csharp
public interface IKeyValueStore
{
    string Alias { get; }
    bool IsReadOnly { get; }

    bool Remove(string key);
    void Clear();
    bool Contains(string key);

    object? Get(Type type, string key);
    void Set(string key, object value);
}
```

### IKeyValueStoreFactory

```csharp
public interface IKeyValueStoreFactory
{
    string[] AvailableStores { get; }
    bool HasStore(string aliasName);
    IKeyValueStore GetStore(string aliasName);
    IKeyValueStore DefaultStore { get; }
    void SetDefaultStore(string aliasName);
}
```

Default store name is `"settings"` (configurable via `KeyValueStoreFactory.DefaultStoreName`).

### IObjectStoreBinder

```csharp
public interface IObjectStoreBinder
{
    // Bind INotifyPropertyChanged object to a named store (or attribute-specified store)
    void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null);

    // Bind to a specific store instance
    void Bind(INotifyPropertyChanged npc, IKeyValueStore store);

    // Unbind an object
    void UnBind(INotifyPropertyChanged npc);

    // Unbind all objects
    void UnBindAll();
}
```

### ISerializer

```csharp
public interface ISerializer
{
    T Deserialize<T>(string value);
    object Deserialize(Type objectType, string value);
    string Serialize(object value);
}
```

Default implementation uses `System.Text.Json`.

### ObjectStoreBinderAttribute

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ObjectStoreBinderAttribute(string storeAlias) : Attribute
{
    public string StoreAlias { get; }
}
```

### StoreExtensions

```csharp
public static class StoreExtensions
{
    // Get with default value
    public static T Get<T>(this IKeyValueStore store, string key, T defaultValue = default);

    // Get required value (throws if not found)
    public static T GetRequired<T>(this IKeyValueStore store, string key);

    // Set or remove if value is null
    public static void SetOrRemove(this IKeyValueStore store, string key, object? value);

    // Set only if key doesn't already exist
    public static bool SetDefault<T>(this IKeyValueStore store, string key, T value);

    // Thread-safe integer increment
    public static int IncrementValue(this IKeyValueStore store, string key = "NextId");
}
```

### ServiceCollectionExtensions (Stores)

```csharp
public static class ServiceCollectionExtensions
{
    // Register core stores (memory + platform-specific settings/secure)
    public static IServiceCollection AddShinyStores(this IServiceCollection services);

    // Register a persistent service bound to a store
    public static IServiceCollection AddPersistentService<TImpl>(
        this IServiceCollection services,
        string? keyValueAlias = null) where TImpl : class, INotifyPropertyChanged;

    public static IServiceCollection AddPersistentService(
        this IServiceCollection services,
        Type implementationType,
        string? keyValueAlias = null);
}
```

### Built-in Store Implementations

| Class | Alias | Platform | Backend |
|-------|-------|----------|---------|
| `MemoryKeyValueStore` | `"memory"` | All | In-memory dictionary |
| `SettingsKeyValueStore` | `"settings"` | Android | SharedPreferences |
| `SettingsKeyValueStore` | `"settings"` | iOS/macOS | NSUserDefaults |
| `SettingsKeyValueStore` | `"settings"` | Windows | ApplicationData.LocalSettings |
| `SecureKeyValueStore` | `"secure"` | Android | EncryptedSharedPreferences (AndroidKeyStore) |
| `SecureKeyValueStore` | `"secure"` | iOS/macOS | Keychain |

---

## Stores Web Module

**Namespace:** `Shiny.Extensions.Stores.Web`

### ServiceCollectionExtensions (Web)

```csharp
public static class ServiceCollectionExtensions
{
    // Register Blazor WebAssembly stores (localStorage + sessionStorage)
    public static IServiceCollection AddShinyWebAssemblyStores(
        this IServiceCollection services);
}
```

### Web Store Implementations

| Class | Alias | Backend |
|-------|-------|---------|
| `LocalStorageKeyValueStore` | `"settings"` | Browser localStorage via JSInterop |
| `SessionStorageKeyValueStore` | `"session"` | Browser sessionStorage via JSInterop |

---

## Web Hosting Module

**Namespace:** `Shiny`

### IInfrastructureModule

```csharp
public interface IInfrastructureModule
{
    void Add(WebApplicationBuilder builder);    // Register services
    void Use(WebApplication app);               // Configure middleware
}
```

### RegistrationExtensions

```csharp
public static class RegistrationExtensions
{
    // Auto-discover modules from AppDomain assemblies
    public static WebApplicationBuilder AddInfrastructureForAppDomain(
        this WebApplicationBuilder builder,
        Func<string, bool>? predicate = null);

    // Discover modules from specific assemblies
    public static WebApplicationBuilder AddInfrastructure(
        this WebApplicationBuilder builder,
        params Assembly[] assemblies);

    // Register specific module instances
    public static WebApplicationBuilder AddInfrastructureModules(
        this WebApplicationBuilder builder,
        params IInfrastructureModule[] modules);

    // Apply all registered module middleware
    public static WebApplication UseInfrastructure(this WebApplication app);
}
```

Module discovery scans assemblies for public, non-abstract classes implementing `IInfrastructureModule`, instantiates them, and calls `Add()` during registration and `Use()` during middleware setup.

---

## Troubleshooting

### Services not being registered
- Ensure `AddShinyServiceRegistry()` is called in your startup
- Verify classes have `[Singleton]`, `[Scoped]`, or `[Transient]` attributes
- If using categories, pass the matching category names to `AddShinyServiceRegistry("category")`
- Clean and rebuild to regenerate the source generator output

### Persistent service not persisting
- Ensure the class implements `INotifyPropertyChanged` and raises `PropertyChanged` events
- Verify `AddPersistentService<T>()` is called (not just `AddSingleton<T>()`)
- Check the store alias matches an available store (`"settings"`, `"secure"`, or `"memory"`)

### Store not found
- Call `AddShinyStores()` before using stores
- For Blazor, call `AddShinyWebAssemblyStores()` instead
- Use `IKeyValueStoreFactory.AvailableStores` to list registered store aliases

### Infrastructure module not discovered
- Module class must be `public` and non-abstract
- Module must implement `IInfrastructureModule`
- If using `AddInfrastructure()`, pass the correct assembly
- If using `AddInfrastructureForAppDomain()`, ensure the assembly DLL is in the output directory
