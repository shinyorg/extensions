namespace Shiny.Extensions.DependencyInjection.Tests;


public class BindSourceGeneratorTests
{
    [Fact]
    public Task GeneratesDefaultStoreBinding()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind]
                    public partial string Theme { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesSecureStoreBinding()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class SecureSettings
                {
                    [Bind("secure")]
                    public partial string Token { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesKeyOverride()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind(Key = "custom-key")]
                    public partial int Counter { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesCustomKeyedStore()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind("my-store")]
                    public partial string Theme { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesMixedBindAndRegistration()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public interface IAppSettings { }

                [Singleton]
                public partial class AppSettings : IAppSettings
                {
                    [Bind]
                    public partial string Theme { get; set; }

                    [Bind("secure")]
                    public partial string Token { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }
}
