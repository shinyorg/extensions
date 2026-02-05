# Shiny Extensions Code Templates

## Service Registration Templates

### Basic Service with Interface

```csharp
// Services/{Name}Service.cs
namespace {Namespace}.Services;

public interface I{Name}Service
{
    {InterfaceMethods}
}

[Singleton]
public class {Name}Service : I{Name}Service
{
    private readonly {Dependencies};

    public {Name}Service({DependencyParameters})
    {
        {DependencyAssignments}
    }

    {Implementation}
}
```

### Scoped Service (per-request)

```csharp
// Services/{Name}Repository.cs
namespace {Namespace}.Services;

[Scoped]
public class {Name}Repository : I{Name}Repository
{
    private readonly DbContext _db;

    public {Name}Repository({Namespace}DbContext db)
    {
        _db = db;
    }

    {Implementation}
}
```

### Keyed Service

```csharp
// Services/{Name}Cache.cs
namespace {Namespace}.Services;

public interface ICache
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
}

[Singleton(KeyedName = "memory")]
public class MemoryCache : ICache
{
    {MemoryCacheImplementation}
}

[Singleton(KeyedName = "distributed")]
public class DistributedCache : ICache
{
    {DistributedCacheImplementation}
}
```

### Category-Based Service

```csharp
// Services/{Name}Feature.cs
namespace {Namespace}.Services;

[Singleton(Category = "{categoryName}")]
public class {Name}Feature : I{Name}Feature
{
    {Implementation}
}

// Registration - only registered when category is requested
// builder.Services.AddShinyServiceRegistry("{categoryName}");
```

### Multi-Interface Service

```csharp
// Services/{Name}Service.cs
namespace {Namespace}.Services;

// Registers against IFirstInterface only (not ISecondInterface)
[Singleton(Type = typeof(IFirstInterface))]
public class {Name}Service : IFirstInterface, ISecondInterface
{
    {Implementation}
}

// Or register as self
[Singleton(AsSelf = true)]
public class {Name}Service : IFirstInterface, ISecondInterface
{
    {Implementation}
}
```

### Startup Task

```csharp
// Tasks/{Name}StartupTask.cs
namespace {Namespace}.Tasks;

[Singleton]
public class {Name}StartupTask : IShinyStartupTask
{
    private readonly {Dependencies};

    public {Name}StartupTask({DependencyParameters})
    {
        {DependencyAssignments}
    }

    public void Start()
    {
        {StartupLogic}
    }
}
```

## Key/Value Store Templates

### Persistent Settings Class

```csharp
// Models/{Name}Settings.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace {Namespace}.Models;

public class {Name}Settings : INotifyPropertyChanged
{
    string {property1} = "{defaultValue}";
    public string {Property1}
    {
        get => {property1};
        set => SetProperty(ref {property1}, value);
    }

    int {property2};
    public int {Property2}
    {
        get => {property2};
        set => SetProperty(ref {property2}, value);
    }

    bool {property3};
    public bool {Property3}
    {
        get => {property3};
        set => SetProperty(ref {property3}, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

// Registration:
// services.AddPersistentService<{Name}Settings>();           // Default store
// services.AddPersistentService<{Name}Settings>("secure");   // Secure store
```

### Persistent Settings with ObservableObject (MAUI/MVVM)

```csharp
// Models/{Name}Settings.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace {Namespace}.Models;

public partial class {Name}Settings : ObservableObject
{
    [ObservableProperty]
    string {property1} = "{defaultValue}";

    [ObservableProperty]
    int {property2};

    [ObservableProperty]
    bool {property3};
}

// Registration:
// services.AddPersistentService<{Name}Settings>();
```

### Persistent Settings with ObjectStoreBinderAttribute

```csharp
// Models/{Name}Settings.cs
using Shiny.Extensions.Stores;

namespace {Namespace}.Models;

[ObjectStoreBinder("secure")]
public partial class {Name}Settings : ObservableObject
{
    [ObservableProperty]
    string apiToken = "";

    [ObservableProperty]
    string refreshToken = "";
}

// Registration:
// services.AddPersistentService<{Name}Settings>();  // Will use "secure" store from attribute
```

### Custom Key/Value Store

```csharp
// Stores/{Name}KeyValueStore.cs
namespace {Namespace}.Stores;

public class {Name}KeyValueStore : IKeyValueStore
{
    public string Alias => "{alias}";
    public bool IsReadOnly => false;

    public bool Remove(string key)
    {
        {RemoveImplementation}
    }

    public void Clear()
    {
        {ClearImplementation}
    }

    public bool Contains(string key)
    {
        {ContainsImplementation}
    }

    public object? Get(Type type, string key)
    {
        {GetImplementation}
    }

    public void Set(string key, object value)
    {
        {SetImplementation}
    }
}

// Registration:
// services.AddSingleton<IKeyValueStore, {Name}KeyValueStore>();
```

### Service Using Store Factory

```csharp
// Services/{Name}Service.cs
namespace {Namespace}.Services;

[Singleton]
public class {Name}Service : I{Name}Service
{
    private readonly IKeyValueStoreFactory _storeFactory;

    public {Name}Service(IKeyValueStoreFactory storeFactory)
    {
        _storeFactory = storeFactory;
    }

    public T GetSetting<T>(string key, T defaultValue = default)
    {
        return _storeFactory.DefaultStore.Get<T>(key, defaultValue);
    }

    public void SaveSetting<T>(string key, T value)
    {
        _storeFactory.DefaultStore.Set(key, value);
    }

    public void SaveSecure<T>(string key, T value)
    {
        _storeFactory.GetStore("secure").Set(key, value);
    }
}
```

## Infrastructure Module Templates

### Basic Infrastructure Module

```csharp
// Infrastructure/{Name}Module.cs
using Microsoft.AspNetCore.Builder;

namespace {Namespace}.Infrastructure;

public class {Name}Module : IInfrastructureModule
{
    public void Add(WebApplicationBuilder builder)
    {
        {ServiceRegistration}
    }

    public void Use(WebApplication app)
    {
        {MiddlewareConfiguration}
    }
}
```

### CORS Module

```csharp
// Infrastructure/CorsModule.cs
namespace {Namespace}.Infrastructure;

public class CorsModule : IInfrastructureModule
{
    public void Add(WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    public void Use(WebApplication app)
    {
        app.UseCors();
    }
}
```

### Authentication Module

```csharp
// Infrastructure/AuthModule.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace {Namespace}.Infrastructure;

public class AuthModule : IInfrastructureModule
{
    public void Add(WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["Auth:Authority"];
                options.Audience = builder.Configuration["Auth:Audience"];
            });
        builder.Services.AddAuthorization();
    }

    public void Use(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
```

### Swagger/OpenAPI Module

```csharp
// Infrastructure/SwaggerModule.cs
namespace {Namespace}.Infrastructure;

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
