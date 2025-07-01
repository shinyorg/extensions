using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shiny.Extensions.DependencyInjection.SourceGenerators;

namespace Shiny.Extensions.DependencyInjection.Tests;


public class DependencyInjectionSourceGeneratorTests
{
    [Fact]
    public Task GeneratesSingletonService()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class MySingletonService
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesTransientService()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Transient)]
                public class MyTransientService
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesScopedService()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Scoped)]
                public class MyScopedService
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesKeyedService()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton, "MyKey")]
                public class MyKeyedService
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesServiceWithSingleInterface()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMyService
                {
                }

                [Service(ServiceLifetime.Singleton)]
                public class MyService : IMyService
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesServiceWithMultipleInterfaces()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMyService
                {
                }

                public interface IMyOtherService
                {
                }

                [Service(ServiceLifetime.Singleton)]
                public class MyService : IMyService, IMyOtherService
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesKeyedServiceWithInterface()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMyService
                {
                }

                [Service(ServiceLifetime.Scoped, "MyKey")]
                public class MyKeyedService : IMyService
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesMultipleServicesInSameNamespace()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IService1
                {
                }

                public interface IService2
                {
                }

                [Service(ServiceLifetime.Singleton)]
                public class Service1 : IService1
                {
                }

                [Service(ServiceLifetime.Transient, "Key1")]
                public class KeyedService1
                {
                }

                [Service(ServiceLifetime.Scoped)]
                public class Service2 : IService1, IService2
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesServicesInDifferentNamespaces()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace Namespace1
            {
                [Service(ServiceLifetime.Singleton)]
                public class Service1
                {
                }
            }

            namespace Namespace2
            {
                public interface IService2
                {
                }

                [Service(ServiceLifetime.Scoped)]
                public class Service2 : IService2
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task IgnoresClassesWithoutServiceAttribute()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public class RegularClass
                {
                }

                [Service(ServiceLifetime.Singleton)]
                public class ServiceClass
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesWithGlobalNamespace()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            [Service(ServiceLifetime.Singleton)]
            public class GlobalService
            {
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesComplexScenario()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace Sample
            {
                public interface IStandardInterface
                {
                }
                
                public interface IStandardInterface2
                {
                }

                [Service(ServiceLifetime.Singleton)]
                public class ImplementationOnly
                {
                }

                [Service(ServiceLifetime.Transient, "ImplOnly")]
                public class KeyedImplementationOnly
                {
                }

                [Service(ServiceLifetime.Singleton)]
                public class StandardImplementation : IStandardInterface
                {
                }

                [Service(ServiceLifetime.Scoped, "Standard")]
                public class KeyedStandardImplementation : IStandardInterface
                {
                }

                [Service(ServiceLifetime.Singleton)]
                public class MultipleImplementation : IStandardInterface, IStandardInterface2
                {
                }

                [Service(ServiceLifetime.Scoped)]
                public class ScopedMultipleImplementation : IStandardInterface, IStandardInterface2
                {
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesWithCustomExtensionMethodName()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class MyService
                {
                }
            }
            """;

        var msBuildProperties = new Dictionary<string, string>
        {
            ["build_property.ShinyDIExtensionMethodName"] = "AddMyCustomServices"
        };

        return TestHelper.Verify(source, msBuildProperties);
    }

    [Fact]
    public Task GeneratesWithCustomNamespace()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class MyService
                {
                }
            }
            """;

        var msBuildProperties = new Dictionary<string, string>
        {
            ["build_property.ShinyDIExtensionNamespace"] = "CustomExtensions"
        };

        return TestHelper.Verify(source, msBuildProperties);
    }

    [Fact]
    public Task GeneratesWithRootNamespaceFallback()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class MyService
                {
                }
            }
            """;

        var msBuildProperties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "MyApp.Extensions"
        };

        return TestHelper.Verify(source, msBuildProperties);
    }

    [Fact]
    public Task GeneratesWithBothCustomNamespaceAndMethodName()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class MyService
                {
                }

                [Service(ServiceLifetime.Transient)]
                public class AnotherService
                {
                }
            }
            """;

        var msBuildProperties = new Dictionary<string, string>
        {
            ["build_property.ShinyDIExtensionNamespace"] = "MyCompany.DI.Extensions",
            ["build_property.ShinyDIExtensionMethodName"] = "RegisterAllServices"
        };

        return TestHelper.Verify(source, msBuildProperties);
    }

    [Fact]
    public Task GeneratesWithNamespacePriorityOrder()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class MyService
                {
                }
            }
            """;

        // ShinyDIExtensionNamespace should take priority over RootNamespace
        var msBuildProperties = new Dictionary<string, string>
        {
            ["build_property.ShinyDIExtensionNamespace"] = "HighPriority.Extensions",
            ["build_property.RootNamespace"] = "LowPriority.Extensions"
        };

        return TestHelper.Verify(source, msBuildProperties);
    }

    [Fact]
    public Task GeneratesWithAssemblyNameFallback()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class MyService
                {
                }
            }
            """;

        // No MSBuild properties provided, should fallback to assembly name
        return TestHelper.Verify(source, assemblyName: "MyCustomAssembly");
    }

    [Fact]
    public Task GeneratesSingleExtensionClassForAllServices()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace FirstNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class FirstService
                {
                }
            }

            namespace SecondNamespace
            {
                [Service(ServiceLifetime.Transient)]
                public class SecondService
                {
                }
            }

            namespace ThirdNamespace
            {
                public interface IThirdService { }

                [Service(ServiceLifetime.Scoped)]
                public class ThirdService : IThirdService
                {
                }
            }
            """;

        var msBuildProperties = new Dictionary<string, string>
        {
            ["build_property.ShinyDIExtensionNamespace"] = "AllServices.Extensions",
            ["build_property.ShinyDIExtensionMethodName"] = "AddAllAssemblyServices"
        };

        return TestHelper.Verify(source, msBuildProperties);
    }

    static class TestHelper
    {
        public static Task Verify(string source, Dictionary<string, string>? msBuildProperties = null, string? assemblyName = null)
        {
            // Parse the test source
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            // Create references for the compilation
            List<PortableExecutableReference> references = new()
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ServiceAttribute).Assembly.Location)
            };

            // Add reference to System.Runtime for .NET 5+ 
            var systemRuntimeLocation = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll");
            if (File.Exists(systemRuntimeLocation))
            {
                references.Add(MetadataReference.CreateFromFile(systemRuntimeLocation));
            }

            // Add other necessary runtime assemblies
            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            var additionalRefs = new[]
            {
                "System.Private.CoreLib.dll",
                "System.Collections.dll",
                "System.Linq.dll",
                "System.Text.Encoding.dll"
            };

            foreach (var refName in additionalRefs)
            {
                var refPath = Path.Combine(runtimeDir, refName);
                if (File.Exists(refPath))
                {
                    references.Add(MetadataReference.CreateFromFile(refPath));
                }
            }

            // Create the compilation
            var compilation = CSharpCompilation.Create(
                assemblyName: assemblyName ?? "Tests",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    specificDiagnosticOptions: new Dictionary<string, ReportDiagnostic>
                    {
                        // Suppress warnings about missing main method
                        ["CS5001"] = ReportDiagnostic.Suppress
                    }));

            // Check for compilation errors first
            var compilationDiagnostics = compilation.GetDiagnostics();
            var errors = compilationDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Any())
            {
                throw new InvalidOperationException($"Compilation errors: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
            }

            // Create the source generator
            var generator = new DependencyInjectionSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
        
            // Add analyzer config options if provided
            if (msBuildProperties is { Count: >0 })
            {
                var optionsProvider = new TestAnalyzerConfigOptionsProvider(msBuildProperties);
                driver = (CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(optionsProvider);
            }
            var result = driver.RunGenerators(compilation);
            var runResult = result.GetRunResult();
            
            return Verifier
                .Verify(runResult)
                .UseDirectory("Snapshots");
        }
    }

    // Test implementation of AnalyzerConfigOptionsProvider for MSBuild properties
    internal class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly Dictionary<string, string> _globalOptions;

        public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(_globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestAnalyzerConfigOptions(new Dictionary<string, string>());
    }

    internal class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public TestAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value!);
        }
    }
}
/*
public class DependencyInjectionTests
   {
       readonly ITestOutputHelper output;
       public DependencyInjectionTests(ITestOutputHelper output) => this.output = output;
   
   
       static IServiceProvider Create(Action<IKeyValueStore>? addSettings = null, Action<IServiceCollection>? addServices = null)
       {
           var settings = new MemoryKeyValueStore();
           addSettings?.Invoke(settings);
   
           var services = new ServiceCollection();
           services.AddLogging();
           services.AddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
           services.AddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
           services.AddSingleton<IKeyValueStore>(settings);
           addServices?.Invoke(services);
           KeyValueStoreFactory.DefaultStoreName = "memory";
   
           var sp = services.BuildServiceProvider(true);
           foreach (var startup in sp.GetServices<IShinyStartupTask>())
               startup.Start();
   
           return sp;
       }
   
   
       static void SetCountKey(IKeyValueStore settings, int value)
       {
           var key = $"{typeof(FullService).FullName}.{nameof(FullService.Count)}";
           settings.Set(key, value);
       }
   
   
       [Fact(DisplayName = "DI - Services Restore & Startup")]
       public void ServiceRestoresStateAndStartsUp()
       {
           var setValue = new Random().Next(1, 9999);
           var postValue = setValue + 1;
   
           var services = Create(
               s => SetCountKey(s, setValue),
               s => s.AddShinyService<FullService>()
           );
           services
               .GetRequiredService<IFullService>()
               .Count
               .Should()
               .Be(postValue);
       }
   
   
       [Fact(DisplayName = "DI - Startup Tasks Run")]
       public void StartupTaskRuns()
       {
           var sp = Create(null, x =>
           {
               x.AddShinyService<TestStartupTask>();
           });
           TestStartupTask.Value.Should().Be(99);
           TestStartupTask.Value = 0;
       }
   }
   
   
   public class TestStartupTask : IShinyStartupTask
   {
       public static int Value { get; set; }
   
   
       public void Start()
       {
           Value = 99;
       }
   }
 */