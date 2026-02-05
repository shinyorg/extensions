# Project Scaffolding Templates

## ASP.NET Project with Shiny Extensions

```
{ProjectName}/
├── Program.cs
├── {ProjectName}.csproj
├── appsettings.json
├── Infrastructure/
│   ├── CorsModule.cs
│   ├── AuthModule.cs
│   └── SwaggerModule.cs
├── Services/
│   ├── I{Name}Service.cs
│   └── {Name}Service.cs
└── Models/
    └── AppSettings.cs
```

**Program.cs:**
```csharp
using System.Reflection;
using Shiny;
using Shiny.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register source-generated services from attributes
builder.Services.AddShinyServiceRegistry();

// Auto-discover and register infrastructure modules
builder.AddInfrastructure(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Apply all infrastructure module middleware
app.UseInfrastructure();

// Run startup tasks
app.Services.RunStartupTasks();

app.Run();
```

**{ProjectName}.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Shiny.Extensions.DependencyInjection" Version="*" />
    <PackageReference Include="Shiny.Extensions.WebHosting" Version="*" />
  </ItemGroup>
</Project>
```

## MAUI Project with Shiny Extensions

```
{ProjectName}/
├── MauiProgram.cs
├── {ProjectName}.csproj
├── App.xaml / App.xaml.cs
├── Services/
│   ├── I{Name}Service.cs
│   └── {Name}Service.cs
├── Models/
│   ├── AppSettings.cs
│   └── SecureSettings.cs
├── ViewModels/
└── Views/
```

**MauiProgram.cs:**
```csharp
using Shiny.Extensions.DependencyInjection;
using Shiny.Extensions.Stores;

namespace {Namespace};

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Register source-generated services
        builder.Services.AddShinyServiceRegistry();

        // Register cross-platform stores (memory, settings, secure)
        builder.Services.AddShinyStores();

        // Register persistent services (auto-persisted to store)
        builder.Services.AddPersistentService<AppSettings>();
        builder.Services.AddPersistentService<SecureSettings>("secure");

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Run startup tasks
        app.Services.RunStartupTasks();

        return app;
    }
}
```

**{ProjectName}.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net9.0-android;net9.0-ios;net9.0-maccatalyst</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UseMaui>true</UseMaui>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Shiny.Extensions.DependencyInjection" Version="*" />
    <PackageReference Include="Shiny.Extensions.Stores" Version="*" />
  </ItemGroup>
</Project>
```

## Blazor WebAssembly Project with Shiny Extensions

```
{ProjectName}/
├── Program.cs
├── {ProjectName}.csproj
├── App.razor
├── _Imports.razor
├── Services/
│   ├── I{Name}Service.cs
│   └── {Name}Service.cs
├── Models/
│   └── AppSettings.cs
├── Components/
│   ├── Layout/
│   └── Pages/
└── wwwroot/
```

**Program.cs:**
```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Shiny.Extensions.DependencyInjection;
using Shiny.Extensions.Stores;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Add HTTP client
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register source-generated services
builder.Services.AddShinyServiceRegistry();

// Register WebAssembly stores (localStorage + sessionStorage)
builder.Services.AddShinyWebAssemblyStores();

// Register persistent services (auto-persisted to localStorage)
builder.Services.AddPersistentService<AppSettings>();

var app = builder.Build();

// Run startup tasks
app.Services.RunStartupTasks();

await app.RunAsync();
```

**{ProjectName}.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Shiny.Extensions.DependencyInjection" Version="*" />
    <PackageReference Include="Shiny.Extensions.Stores.Web" Version="*" />
  </ItemGroup>
</Project>
```

## Console/Worker Project with Shiny Extensions

```
{ProjectName}/
├── Program.cs
├── {ProjectName}.csproj
├── Services/
│   ├── I{Name}Service.cs
│   └── {Name}Service.cs
└── Workers/
    └── {Name}Worker.cs
```

**Program.cs:**
```csharp
using Shiny.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Register source-generated services
builder.Services.AddShinyServiceRegistry();

var host = builder.Build();

// Run startup tasks
host.Services.RunStartupTasks();

host.Run();
```

## Feature-Based Organization

For larger projects, organize by feature:

```
Features/
├── Users/
│   ├── IUserService.cs
│   ├── UserService.cs          // [Scoped]
│   ├── UserRepository.cs       // [Scoped]
│   └── UserSettings.cs         // Persistent service
├── Products/
│   ├── IProductService.cs
│   ├── ProductService.cs       // [Singleton]
│   └── ProductCache.cs         // [Singleton(KeyedName = "products")]
├── Infrastructure/
│   ├── CorsModule.cs           // IInfrastructureModule
│   ├── SwaggerModule.cs        // IInfrastructureModule
│   └── AuthModule.cs           // IInfrastructureModule
└── Shared/
    ├── AppSettings.cs           // Persistent INotifyPropertyChanged
    └── SecureSettings.cs        // [ObjectStoreBinder("secure")]
```
