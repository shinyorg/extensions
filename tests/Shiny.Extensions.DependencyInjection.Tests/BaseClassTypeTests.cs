namespace Shiny.Extensions.DependencyInjection.Tests;

public class BaseClassTypeTests
{
    [Fact]
    public Task ServiceWithTypeSpecifiedAsBaseClass()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public abstract class BaseService
                {
                    public abstract string GetValue();
                }

                public interface IMyService
                {
                    string GetValue();
                }

                [Singleton(Type = typeof(BaseService))]
                public class MyService : BaseService, IMyService
                {
                    public override string GetValue() => "Hello from service";
                }
            }
            """;
        
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ServiceWithTypeSpecifiedAsInterface()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public abstract class BaseService
                {
                    public abstract string GetValue();
                }

                public interface IMyService
                {
                    string GetValue();
                }

                [Singleton(Type = typeof(IMyService))]
                public class MyService : BaseService, IMyService
                {
                    public override string GetValue() => "Hello from service";
                }
            }
            """;
        
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ServiceWithoutTypeSpecified()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public abstract class BaseService
                {
                    public abstract string GetValue();
                }

                public interface IMyService
                {
                    string GetValue();
                }

                [Singleton]
                public class MyService : BaseService, IMyService
                {
                    public override string GetValue() => "Hello from service";
                }
            }
            """;
        
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ServiceWithAsSelfTrue()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;
            using Shiny.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public abstract class BaseService
                {
                    public abstract string GetValue();
                }

                public interface IMyService
                {
                    string GetValue();
                }

                [Singleton(AsSelf = true)]
                public class MyService : BaseService, IMyService
                {
                    public override string GetValue() => "Hello from service";
                }
            }
            """;
        
        return TestHelper.Verify(source);
    }
}
