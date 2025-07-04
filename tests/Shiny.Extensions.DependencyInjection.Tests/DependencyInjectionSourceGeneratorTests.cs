using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shiny.Extensions.DependencyInjection.SourceGenerators;

namespace Shiny.Extensions.DependencyInjection.Tests;


public class DependencyInjectionSourceGeneratorTests
{
    [Fact]
    public Task GeneratesForRecords()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public record MyRecordService();
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForRecordWithInterface()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMyService
                {
                    string GetValue();
                }

                [Service(ServiceLifetime.Transient)]
                public record MyRecordService() : IMyService
                {
                    public string GetValue() => "Hello from record";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForRecordWithMultipleInterfaces()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMyService
                {
                    string GetValue();
                }

                public interface IMyOtherService
                {
                    void DoSomething();
                }

                [Service(ServiceLifetime.Scoped)]
                public record MyRecordService() : IMyService, IMyOtherService
                {
                    public string GetValue() => "Hello from record";
                    public void DoSomething() { }
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForKeyedRecord()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMyService
                {
                    string GetValue();
                }

                [Service(ServiceLifetime.Singleton, "RecordKey")]
                public record MyKeyedRecordService() : IMyService
                {
                    public string GetValue() => "Hello from keyed record";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForGenericRecord()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IRepository<T>
                {
                    T Get(int id);
                }

                [Service(ServiceLifetime.Scoped)]
                public record GenericRecordRepository<T>() : IRepository<T>
                {
                    public T Get(int id) => default(T);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForRecordWithParameters()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMyService
                {
                    string Name { get; }
                    int Value { get; }
                }

                [Service(ServiceLifetime.Transient)]
                public record MyParameterizedRecord(string Name, int Value) : IMyService;
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForMixedClassesAndRecords()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IService1
                {
                    void Method1();
                }

                public interface IService2
                {
                    void Method2();
                }

                [Service(ServiceLifetime.Singleton)]
                public class ClassService : IService1
                {
                    public void Method1() { }
                }

                [Service(ServiceLifetime.Transient)]
                public record RecordService() : IService2
                {
                    public void Method2() { }
                }

                [Service(ServiceLifetime.Scoped, "MixedKey")]
                public record KeyedRecordService(string Data) : IService1
                {
                    public void Method1() { }
                }
            }
            """;

        return TestHelper.Verify(source);
    }

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

    // Open Generic Tests
    [Fact]
    public Task GeneratesOpenGenericServiceWithoutInterface()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class GenericService<T>
                {
                    public T GetValue() => default(T);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesOpenGenericServiceWithSingleInterface()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IRepository<T>
                {
                    T Get(int id);
                }

                [Service(ServiceLifetime.Scoped)]
                public class Repository<T> : IRepository<T>
                {
                    public T Get(int id) => default(T);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesOpenGenericServiceWithMultipleInterfaces()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IRepository<T>
                {
                    T Get(int id);
                }

                public interface IGenericService<T>
                {
                    void Process(T item);
                }

                [Service(ServiceLifetime.Transient)]
                public class GenericService<T> : IRepository<T>, IGenericService<T>
                {
                    public T Get(int id) => default(T);
                    public void Process(T item) { }
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesKeyedOpenGenericService()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IGenericHandler<T>
                {
                    void Handle(T item);
                }

                [Service(ServiceLifetime.Singleton, "special")]
                public class SpecialGenericHandler<T> : IGenericHandler<T>
                {
                    public void Handle(T item) { }
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesKeyedOpenGenericServiceWithoutInterface()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Transient, "mykey")]
                public class KeyedGenericService<T>
                {
                    public T Process() => default(T);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesMultipleTypeParameterOpenGeneric()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IConverter<TInput, TOutput>
                {
                    TOutput Convert(TInput input);
                }

                [Service(ServiceLifetime.Scoped)]
                public class GenericConverter<TInput, TOutput> : IConverter<TInput, TOutput>
                {
                    public TOutput Convert(TInput input) => default(TOutput);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesMixedOpenGenericAndClosedServices()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IRepository<T>
                {
                    T Get(int id);
                }

                public interface ILogger
                {
                    void Log(string message);
                }

                [Service(ServiceLifetime.Singleton)]
                public class Repository<T> : IRepository<T>
                {
                    public T Get(int id) => default(T);
                }

                [Service(ServiceLifetime.Transient)]
                public class Logger : ILogger
                {
                    public void Log(string message) { }
                }

                [Service(ServiceLifetime.Scoped, "cached")]
                public class CachedRepository<T> : IRepository<T>
                {
                    public T Get(int id) => default(T);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesOpenGenericWithConstraints()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;
            using System;

            namespace TestNamespace
            {
                public interface IEntity
                {
                    int Id { get; set; }
                }

                public interface IRepository<T> where T : class, IEntity
                {
                    T GetById(int id);
                }

                [Service(ServiceLifetime.Scoped)]
                public class EntityRepository<T> : IRepository<T> where T : class, IEntity
                {
                    public T GetById(int id) => default(T);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesOpenGenericWithThreeTypeParameters()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface ITripleProcessor<T1, T2, T3>
                {
                    T3 Process(T1 input1, T2 input2);
                }

                [Service(ServiceLifetime.Transient)]
                public class TripleProcessor<T1, T2, T3> : ITripleProcessor<T1, T2, T3>
                {
                    public T3 Process(T1 input1, T2 input2) => default(T3);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesOpenGenericWithFourTypeParameters()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IQuadProcessor<T1, T2, T3, T4>
                {
                    T4 Process(T1 input1, T2 input2, T3 input3);
                }

                [Service(ServiceLifetime.Singleton)]
                public class QuadProcessor<T1, T2, T3, T4> : IQuadProcessor<T1, T2, T3, T4>
                {
                    public T4 Process(T1 input1, T2 input2, T3 input3) => default(T4);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesKeyedOpenGenericWithMultipleTypeParameters()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMapper<TSource, TDestination>
                {
                    TDestination Map(TSource source);
                }

                [Service(ServiceLifetime.Scoped, "auto")]
                public class AutoMapper<TSource, TDestination> : IMapper<TSource, TDestination>
                {
                    public TDestination Map(TSource source) => default(TDestination);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesOpenGenericWithMultipleInterfacesAndDifferentArities()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface ISingleGeneric<T>
                {
                    T GetSingle();
                }

                public interface IDualGeneric<T1, T2>
                {
                    T2 Process(T1 input);
                }

                public interface ITripleGeneric<T1, T2, T3>
                {
                    T3 Transform(T1 input1, T2 input2);
                }

                [Service(ServiceLifetime.Transient)]
                public class MultiArityService<T1, T2, T3> : ISingleGeneric<T1>, IDualGeneric<T1, T2>, ITripleGeneric<T1, T2, T3>
                {
                    public T1 GetSingle() => default(T1);
                    public T2 Process(T1 input) => default(T2);
                    public T3 Transform(T1 input1, T2 input2) => default(T3);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesMixedArityOpenGenerics()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IRepository<T>
                {
                    T Get(int id);
                }

                public interface IConverter<TInput, TOutput>
                {
                    TOutput Convert(TInput input);
                }

                public interface IProcessor<T1, T2, T3>
                {
                    T3 Process(T1 input1, T2 input2);
                }

                [Service(ServiceLifetime.Singleton)]
                public class SingleGeneric<T> : IRepository<T>
                {
                    public T Get(int id) => default(T);
                }

                [Service(ServiceLifetime.Scoped)]
                public class DualGeneric<TInput, TOutput> : IConverter<TInput, TOutput>
                {
                    public TOutput Convert(TInput input) => default(TOutput);
                }

                [Service(ServiceLifetime.Transient)]
                public class TripleGeneric<T1, T2, T3> : IProcessor<T1, T2, T3>
                {
                    public T3 Process(T1 input1, T2 input2) => default(T3);
                }

                [Service(ServiceLifetime.Scoped, "special")]
                public class KeyedDualGeneric<TIn, TOut> : IConverter<TIn, TOut>
                {
                    public TOut Convert(TIn input) => default(TOut);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesOpenGenericWithoutInterfaceMultipleTypeParams()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Singleton)]
                public class StandaloneGeneric<T1, T2>
                {
                    public T2 Process(T1 input) => default(T2);
                }

                [Service(ServiceLifetime.Transient, "multi")]
                public class KeyedStandaloneGeneric<T1, T2, T3>
                {
                    public T3 Transform(T1 input1, T2 input2) => default(T3);
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesComplexGenericHierarchy()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IBaseProcessor<T>
                {
                    T Process();
                }

                public interface IAdvancedProcessor<T1, T2> : IBaseProcessor<T2>
                {
                    T2 AdvancedProcess(T1 input);
                }

                public interface ISuperProcessor<T1, T2, T3> : IAdvancedProcessor<T1, T2>
                {
                    T3 SuperProcess(T1 input1, T2 input2);
                }

                [Service(ServiceLifetime.Scoped)]
                public class HierarchicalProcessor<T1, T2, T3> : ISuperProcessor<T1, T2, T3>
                {
                    public T2 Process() => default(T2);
                    public T2 AdvancedProcess(T1 input) => default(T2);
                    public T3 SuperProcess(T1 input1, T2 input2) => default(T3);
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

    [Fact]
    public Task GeneratesWithInternalAccessor()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IMyService
                {
                    void DoWork();
                }

                [Service(ServiceLifetime.Singleton)]
                public class MyService : IMyService
                {
                    public void DoWork() { }
                }

                [Service(ServiceLifetime.Transient)]
                public class AnotherService
                {
                }
            }
            """;

        var msBuildProperties = new Dictionary<string, string>
        {
            ["build_property.ShinyDIUseInternalAccessor"] = "true"
        };

        return TestHelper.Verify(source, msBuildProperties);
    }

    [Fact]
    public Task GeneratesWithInternalAccessorAndCustomNamespace()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                [Service(ServiceLifetime.Scoped)]
                public class MyInternalService
                {
                }
            }
            """;

        var msBuildProperties = new Dictionary<string, string>
        {
            ["build_property.ShinyDIUseInternalAccessor"] = "true",
            ["build_property.ShinyDIExtensionNamespace"] = "Internal.Extensions",
            ["build_property.ShinyDIExtensionMethodName"] = "AddInternalServices"
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
        private readonly Dictionary<string, string> globalOptions;

        public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
        {
            this.globalOptions = globalOptions;
        }

        public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestAnalyzerConfigOptions(new Dictionary<string, string>());
    }

    internal class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> options;

        public TestAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            this.options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return options.TryGetValue(key, out value!);
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