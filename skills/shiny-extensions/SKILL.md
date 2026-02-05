---
name: shiny-extensions
description: Generate and configure Shiny Extensions for .NET - attribute-driven DI registration with source generators, cross-platform key/value stores, and ASP.NET infrastructure modules
auto_invoke: true
triggers:
  - shiny extensions
  - ServiceAttribute
  - SingletonAttribute
  - ScopedAttribute
  - TransientAttribute
  - AddShinyServiceRegistry
  - IKeyValueStore
  - IObjectStoreBinder
  - IKeyValueStoreFactory
  - AddShinyStores
  - AddPersistentService
  - ObjectStoreBinderAttribute
  - IInfrastructureModule
  - AddInfrastructure
  - UseInfrastructure
  - IShinyStartupTask
  - Shiny.Extensions.DependencyInjection
  - Shiny.Extensions.Stores
  - Shiny.Extensions.WebHosting
---

# Shiny Extensions Skill

You are an expert in Shiny Extensions, a set of .NET libraries providing attribute-driven dependency injection with source generators, cross-platform key/value stores, and ASP.NET infrastructure modules.

## When to Use This Skill

Invoke this skill when the user wants to:
- Register services using `[Singleton]`, `[Scoped]`, or `[Transient]` attributes with source generation
- Configure the Shiny DI service registry in their application
- Use cross-platform key/value stores (settings, secure storage, memory)
- Bind `INotifyPropertyChanged` objects to persistent storage
- Create ASP.NET infrastructure modules with `IInfrastructureModule`
- Set up web hosting with modular service/middleware registration
- Use key/value stores in Blazor WebAssembly (localStorage, sessionStorage)

## Library Overview

**Documentation**: https://shinylib.net
**Repository**: https://github.com/shinyorg/Shiny.Extensions

Shiny Extensions is a collection of three modules:

| Module | Package | Purpose |
|--------|---------|---------|
| Dependency Injection | `Shiny.Extensions.DependencyInjection` | Attribute-driven service registration with source generators |
| Stores | `Shiny.Extensions.Stores` | Cross-platform key/value store abstraction |
| Stores (Web) | `Shiny.Extensions.Stores.Web` | Blazor WebAssembly localStorage/sessionStorage |
| Web Hosting | `Shiny.Extensions.WebHosting` | ASP.NET modular infrastructure modules |

## Module 1: Dependency Injection

### Registration Attributes

Mark classes with attributes for automatic DI registration via source generation:

```csharp
[Singleton]                                    // Singleton lifetime
public class MyService : IMyService { }

[Scoped]                                       // Scoped lifetime
public class MyRepository : IRepository { }

[Transient]                                    // Transient lifetime
public class MyFactory : IFactory { }
```

### Attribute Properties

All registration attributes support these properties:

| Property | Type | Description |
|----------|------|-------------|
| `AsSelf` | `bool` | Register as the class itself rather than its interface |
| `Type` | `Type` | Register against a specific interface (when class implements multiple) |
| `KeyedName` | `string?` | Register as a keyed service |
| `Category` | `string?` | Category for conditional registration |
| `TryAdd` | `bool` | Use TryAdd semantics (won't replace existing registrations) |

### Examples

```csharp
// Register as the class itself
[Singleton(AsSelf = true)]
public class AppState { }

// Register against a specific interface
[Singleton(Type = typeof(ISpecificInterface))]
public class MultiInterfaceService : ISpecificInterface, IOtherInterface { }

// Keyed service
[Singleton(KeyedName = "primary")]
public class PrimaryCache : ICache { }

// Category-based conditional registration
[Singleton(Category = "premium")]
public class PremiumFeature : IFeature { }

// TryAdd semantics
[Singleton(TryAdd = true)]
public class DefaultLogger : ILogger { }
```

### Setup

```csharp
// Register all source-generated services
builder.Services.AddShinyServiceRegistry();

// Register with specific categories only
builder.Services.AddShinyServiceRegistry("premium", "analytics");
```

### Startup Tasks

Implement `IShinyStartupTask` for code that runs after DI is configured:

```csharp
[Singleton]
public class DatabaseMigrationTask : IShinyStartupTask
{
    public void Start()
    {
        // Run migrations, seed data, etc.
    }
}

// Execute all startup tasks
app.Services.RunStartupTasks();
```

### Helper Extensions

```csharp
// Register a singleton against all implemented interfaces
services.AddSingletonAsImplementedInterfaces<MyService>();
services.AddSingletonAsImplementedInterfaces<MyService>("keyName");

// Register scoped against all implemented interfaces
services.AddScopedAsImplementedInterfaces<MyService>();

// Check registrations
bool hasService = services.HasService<IMyService>();
bool hasImpl = services.HasImplementation<MyService>();

// Lazy resolution
Lazy<IMyService> lazy = services.GetLazyService<IMyService>(required: true);
```

## Module 2: Key/Value Stores

### Built-in Store Aliases

| Alias | Platform | Implementation |
|-------|----------|---------------|
| `"memory"` | All | In-memory dictionary |
| `"settings"` | Android | SharedPreferences |
| `"settings"` | iOS/macOS | NSUserDefaults |
| `"settings"` | Windows | ApplicationData.LocalSettings |
| `"settings"` | Blazor | localStorage |
| `"secure"` | Android | EncryptedSharedPreferences |
| `"secure"` | iOS/macOS | Keychain |
| `"session"` | Blazor | sessionStorage |

### Setup

```csharp
// Mobile/Desktop - registers memory, settings, and secure stores
services.AddShinyStores();

// Blazor WebAssembly - adds localStorage and sessionStorage
services.AddShinyWebAssemblyStores();
```

### Using Stores Directly

```csharp
// Via IKeyValueStoreFactory
public class MyService(IKeyValueStoreFactory storeFactory)
{
    public void SaveSetting(string key, string value)
    {
        var store = storeFactory.GetStore("settings");
        store.Set(key, value);
    }

    public T GetSetting<T>(string key, T defaultValue = default)
    {
        var store = storeFactory.DefaultStore;
        return store.Get<T>(key, defaultValue);
    }
}
```

### Persistent Services (Object-Store Binding)

Bind `INotifyPropertyChanged` objects to a store so property changes are automatically persisted:

```csharp
// Register a persistent service
services.AddPersistentService<AppSettings>();                    // Uses default store
services.AddPersistentService<SecureSettings>("secure");         // Uses secure store

// The class
public class AppSettings : INotifyPropertyChanged
{
    string theme = "light";
    public string Theme
    {
        get => theme;
        set { theme = value; OnPropertyChanged(); }
    }

    // ... INotifyPropertyChanged implementation
}
```

You can also target a specific store with the attribute:

```csharp
[ObjectStoreBinder("secure")]
public class SecureSettings : INotifyPropertyChanged
{
    // Properties are automatically persisted to the secure store
}
```

### Store Extension Methods

```csharp
store.Get<T>(key, defaultValue);        // Get with default
store.GetRequired<T>(key);              // Throws if not found
store.SetOrRemove(key, value);          // Removes if value is null
store.SetDefault<T>(key, value);        // Only sets if key doesn't exist
store.IncrementValue(key);              // Thread-safe integer increment
```

## Module 3: Web Hosting Infrastructure

### Creating Infrastructure Modules

```csharp
public class SwaggerModule : IInfrastructureModule
{
    public void Add(WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    public void Use(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}
```

### Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Auto-discover modules from assemblies
builder.AddInfrastructure(Assembly.GetExecutingAssembly());

// Or auto-discover from all loaded assemblies
builder.AddInfrastructureForAppDomain();

// Or register specific modules
builder.AddInfrastructureModules(new SwaggerModule(), new CorsModule());

var app = builder.Build();

// Apply all module middleware
app.UseInfrastructure();

app.Run();
```

## Code Generation Instructions

When generating code with Shiny Extensions:

### 1. Service Registration
- Always use attributes (`[Singleton]`, `[Scoped]`, `[Transient]`) over manual registration
- Choose appropriate lifetime: Singleton for stateless services, Scoped for per-request (DbContext), Transient for lightweight factories
- Use `Category` for optional features that may not always be registered
- Use `KeyedName` when multiple implementations of the same interface exist

### 2. Stores
- Use `AddShinyStores()` for mobile/desktop, `AddShinyWebAssemblyStores()` for Blazor
- Use `AddPersistentService<T>()` for auto-persisting settings classes
- Always implement `INotifyPropertyChanged` for persistent services
- Specify the store alias when targeting secure storage

### 3. Infrastructure Modules
- One module per concern (CORS, auth, swagger, etc.)
- Keep `Add()` for service registration and `Use()` for middleware
- Use `AddInfrastructure()` with assembly scanning for automatic discovery

## Best Practices

1. **Use source generation** - Always prefer `[Singleton]`/`[Scoped]`/`[Transient]` attributes over manual `services.Add*()` calls
2. **Call AddShinyServiceRegistry()** - Required to activate source-generated registrations
3. **Appropriate lifetimes** - Singleton for stateless, Scoped for DbContext/unit-of-work, Transient for factories
4. **Use persistent services** - For app settings that should survive restarts, use `AddPersistentService<T>()`
5. **Target secure store** - Always use the `"secure"` store alias for sensitive data (tokens, credentials)
6. **Modular web hosting** - Create one `IInfrastructureModule` per cross-cutting concern for clean separation
7. **Use keyed services** - When multiple implementations exist, use `KeyedName` to disambiguate

## Reference Files

For detailed templates and examples, see:
- `reference/api-reference.md` - Full API surface, interfaces, and attributes
- `reference/templates.md` - Code generation templates
- `reference/scaffolding.md` - Project structure templates

## Common Packages

```bash
dotnet add package Shiny.Extensions.DependencyInjection   # DI attributes + source generators
dotnet add package Shiny.Extensions.Stores                 # Cross-platform key/value stores
dotnet add package Shiny.Extensions.Stores.Web             # Blazor localStorage/sessionStorage
dotnet add package Shiny.Extensions.WebHosting             # ASP.NET infrastructure modules
```
