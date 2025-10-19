# Shiny Extensions

## Dependency Injection Extensions

* Source generate all attributed classes to a single add file - saves you the boilerplate
* Extension methods for registering a dependency against multiple interfaces
* Extension methods for startup tasks (different to hosted services that don't work on mobile)
* Supports multiple interfaces
* Supports open generics
* Supports keyed services

### The Results

THIS:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection;

// given the following code from a user
namespace Sample
{
    [Singleton]
    public record MyStandardSingletonRecord : IStandardInterface;

    [Scoped]
    public record MyStandardScopedRecord;
    
    
    public interface IStandardInterface;

    public interface IStandardInterface2;

    [Singleton]
    public class ImplementationOnly;

    [Transient(KeyedName = "ImplOnly")]
    public class KeyedImplementationOnly;


    [Singleton(TryAdd = true)]
    public class StandardImplementation : IStandardInterface;

    [Scoped(KeyedName = "Standard")]
    public class KeyedStandardImplementation : IStandardInterface;

    [Singleton]
    public class MultipleImplementation : IStandardInterface, IStandardInterface2;

    [Scoped]
    public class ScopedMultipleImplementation : IStandardInterface, IStandardInterface2;


    [Scoped(KeyedName = "KeyedGeneric", TryAdd = true)]
    public class TestGeneric<T1, T2>
    {
        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }
    }


    [Singleton(Category = "DEV1")]
    public class DevCategoryService;
    
    [Singleton(Category = "PROD")]
    public class ProdCategoryService;


    [Singleton(AsSelf = true)]
    public class AsSelfTest : IStandardInterface;
    
    
    [Singleton(Type = typeof(IStandardInterface2))]
    public class SpecificTest : IStandardInterface, IStandardInterface2;
}
```

GENERATES THIS:
```csharp
internal static class __ShinyServicesModule
{
    [global::System.Runtime.CompilerServices.ModuleInitializer]
    public static void Run()
    {
        global::Shiny.Extensions.DependencyInjection.Internals.ServiceRegistry.RegisterCallback((services, categories) => {
            services.AddSingleton<global::Sample.IStandardInterface, global::Sample.MyStandardSingletonRecord>();
            services.AddScoped<global::Sample.MyStandardScopedRecord>();
            services.AddSingleton<global::Sample.ImplementationOnly>();
            services.AddKeyedTransient<global::Sample.KeyedImplementationOnly>("ImplOnly");
            services.TryAddSingleton<global::Sample.IStandardInterface, global::Sample.StandardImplementation>();
            services.AddKeyedScoped<global::Sample.IStandardInterface, global::Sample.KeyedStandardImplementation>("Standard");
            global::Shiny.Extensions.DependencyInjection.ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces<global::Sample.MultipleImplementation>(services);
            global::Shiny.Extensions.DependencyInjection.ServiceCollectionExtensions.AddScopedAsImplementedInterfaces<global::Sample.ScopedMultipleImplementation>(services);
            services.TryAddKeyedScoped(typeof(global::Sample.TestGeneric<,>), "KeyedGeneric");
            if (categories?.Any(x => x.Equals("DEV1", global::System.StringComparison.OrdinalIgnoreCase)) == true)
            {
                services.AddSingleton<global::Sample.DevCategoryService>();
            }
            if (categories?.Any(x => x.Equals("PROD", global::System.StringComparison.OrdinalIgnoreCase)) == true)
            {
                services.AddSingleton<global::Sample.ProdCategoryService>();
            }
            services.AddSingleton<global::Sample.AsSelfTest>();
            services.AddSingleton<global::Sample.IStandardInterface2, global::Sample.SpecificTest>();
        });
    }
}
```

### Setup

1. Install the NuGet package `Shiny.Extensions.DependencyInjection`
2. Add the following using directive:
   ```csharp
   // during your app startup - use your service collection 
   builder.Services.AddShinyServiceRegistry();
   ```
3. Add the `[Service(ServiceLifetime.Singleton, "optional key")]` attribute to your classes and specify the lifetime and optional key

## Stores
* Key/value store with support for
  * Android/iOS/Windows - Preferences & Secure Storage
  * Web - Local Storage & Session Storage
  * In Memory
* Object binder binds INotifyPropertyChanged against a key/value store to persist object changes across sessions
* Simply implement IKeyValueStore to create your own store

### Setup

1. Install the NuGet package `Shiny.Extensions.Stores`
2. Add the following using directive:
  ```csharp
  // during your app startup - use your service collection 
  
  builder.Services.AddPersistentService<MyNotifyPropertyChangedObject>("secure"); // optional: default to `settings`
  ```
3. Inject the MyNotifyPropertyChangedObject into your view model or service.  Set properties and they will be persisted automatically.
4. To bypass reflection and make binding super fast - use [Shiny Reflector](https://github.com/shinyorg/reflector) to remove the need for reflection.  It is already built into the Shiny.Extensions.Stores package, so you can use it directly.  Just mark `[Reflector]` on your class and make your class partial.

### Available Stores Per Platform

| Platform     | Store Alias | Description                         |
|--------------|-------------|-------------------------------------|
| Android      | settings    | Preferences store                   |
| Android      | secure      | Secure Storage                      |
| iOS          | settings    | Preferences store                   |
| iOS          | secure      | Secure Storage                      |
| WebAssembly  | settings    | Local Storage                       |
| WebAssembly  | session     | Session Storage                     |
| All          | Memory      | In Memory store - great for testing |

> [!NOTE]
> For WebAssembly, install the `Shiny.Extensions.Stores.Web` package and add `services.AddWebAssemblyStores()` to your service collection.

## Web Hosting Extensions
* Merges service container build and post build scenarios into a single class
* All IInfrastructureModule implementations are automatically detected and run

### Setup
1. Install the NuGet package `Shiny.Extensions.WebHosting`
2. Add an infrastructure module by implementing `IInfrastructureModule`:
   ```csharp
   using Shiny.Extensions.WebHosting;

   public class MyInfrastructureModule : IInfrastructureModule
   {
       public void Add(WebApplicationBuilder builder)
       {
           // Register your services here
       }

       public void Configure(WebApplication app)
       {
           // Configure your application here
       }
   }
   ```
3. In your application hosting startup, add the following:
   ```csharp
   using Shiny.Extensions.WebHosting;

   var builder = WebApplication.CreateBuilder(args);
   builder.AddInfrastructure(params Assembly[] assemblies)(); // this scans the assemblies for IInfrastructureModule implementations and runs Add methods
   // OR
   builder.AddInfrastructureModules(params IInfrastructureModule[] modules); // this doesn't use reflection
   
   var app = builder.Build();
   app.UseInfrastructure(); // this runs all IInfrastructureModule.Use methods
   ```


## Additional Libraries Used
* [Shiny Reflector](https://github.com/shinyorg/reflector) - Reflection without the actual reflection