namespace Shiny.Extensions.DependencyInjection.Tests;


public class DependencyInjectionSourceGeneratorTests
{
    [Fact]
    public Task GeneratesMsbuildProperties()
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

                         [Singleton]
                         public class MySingletonService : IMyService
                         {
                             public string GetValue() => "Hello from singleton";
                         }
                     }
                     """;
        
        return TestHelper.Verify(source,
            new Dictionary<string, string>
            {
                { "ShinyDIExtensionMethodName", "AddMyServices" },
                { "ShinyDIExtensionNamespace", "ThisIsMyNamespace" },
                { "ShinyDIExtensionInternalAccessor", "true" }
            });
    }
    
    [Fact]
    public Task GeneratesForSingletonAttribute()
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

                [Singleton]
                public class MySingletonService : IMyService
                {
                    public string GetValue() => "Hello from singleton";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForScopedAttribute()
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

                [Scoped]
                public class MyScopedService : IMyService
                {
                    public string GetValue() => "Hello from scoped";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForTransientAttribute()
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

                [Transient]
                public class MyTransientService : IMyService
                {
                    public string GetValue() => "Hello from transient";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForAsSelfTrue()
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

                [Singleton(AsSelf = true)]
                public class MySelfRegisteredService : IMyService
                {
                    public string GetValue() => "Hello from self-registered";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForAsSelfFalse()
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

                [Singleton(AsSelf = false)]
                public class MyInterfaceRegisteredService : IMyService
                {
                    public string GetValue() => "Hello from interface-registered";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForAsSelfWithMultipleInterfaces()
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

                [Singleton(AsSelf = true)]
                public class MyMultiInterfaceAsSelfService : IMyService, IMyOtherService
                {
                    public string GetValue() => "Hello";
                    public void DoSomething() { }
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForServiceAttributeWithAsSelf()
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

                [Service(ServiceLifetime.Scoped, AsSelf = true)]
                public class MyServiceWithAsSelf : IMyService
                {
                    public string GetValue() => "Hello from service with AsSelf";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesForServiceWithTypeProperty()
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
                    void DoOtherWork();
                }

                [Singleton(Type = typeof(IMyService))]
                public class MyMultiInterfaceService : IMyService, IMyOtherService
                {
                    public string GetValue() => "Hello";
                    public void DoOtherWork() { }
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesWithKeyedNameAndCategory()
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

                [Scoped(KeyedName = "MyKey", Category = "MyCategory")]
                public class MyKeyedCategorizedService : IMyService
                {
                    public string GetValue() => "Hello with key and category";
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesMultipleNewAttributeTypes()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public interface IService1 { }
                public interface IService2 { }
                public interface IService3 { }

                [Singleton]
                public class SingletonService : IService1 { }

                [Scoped]
                public class ScopedService : IService2 { }

                [Transient]
                public class TransientService : IService3 { }

                [Singleton(AsSelf = true)]
                public class AsSelfService : IService1 { }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ReportsErrorForConflictingAsSelfAndType()
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

                [Singleton(AsSelf = true, Type = typeof(IMyService))]
                public class ConflictingService : IMyService
                {
                    public string GetValue() => "This should error";
                }
            }
            """;

        return TestHelper.Verify(source);
    }
}
