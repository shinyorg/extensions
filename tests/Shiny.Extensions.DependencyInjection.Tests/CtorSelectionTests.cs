namespace Shiny.Extensions.DependencyInjection.Tests;


public class CtorSelectionTests
{
    [Fact]
    public Task ConstructorWithDependencies()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public interface IDep { }
                public interface IService { }

                [Singleton]
                public class MyService : IService
                {
                    public MyService(IDep dep) { }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task MultipleConstructorsPicksLongest()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public interface IDep1 { }
                public interface IDep2 { }
                public interface IService { }

                [Singleton]
                public class MyService : IService
                {
                    public MyService() { }
                    public MyService(IDep1 a) { }
                    public MyService(IDep1 a, IDep2 b) { }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task ActivatorUtilitiesConstructorWins()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny;

            namespace TestNamespace
            {
                public interface IDep1 { }
                public interface IDep2 { }
                public interface IService { }

                [Singleton]
                public class MyService : IService
                {
                    [ActivatorUtilitiesConstructor]
                    public MyService(IDep1 a) { }
                    public MyService(IDep1 a, IDep2 b) { }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task FromKeyedServicesAttribute()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny;

            namespace TestNamespace
            {
                public interface ICache { }
                public interface IService { }

                [Singleton]
                public class MyService : IService
                {
                    public MyService([FromKeyedServices("primary")] ICache cache) { }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task ServiceProviderParameter()
    {
        var source = """
            using System;
            using Shiny;

            namespace TestNamespace
            {
                public interface IService { }

                [Singleton]
                public class MyService : IService
                {
                    public MyService(IServiceProvider sp) { }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task MultiInterfaceEmitsForwarders()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public interface IFoo { }
                public interface IBar { }

                [Singleton]
                public class MyService : IFoo, IBar
                {
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }
}
