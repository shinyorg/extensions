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


    [Fact]
    public Task GeneratesNotifyClass()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                [BindNotify]
                public partial class AppSettings
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


    [Fact]
    public Task GeneratesPrivateSet()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind]
                    public partial string Theme { get; private set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesProtectedSet()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind]
                    public partial string Theme { get; protected set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesStringDefault()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind(Default = "dark")]
                    public partial string Theme { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesIntDefault()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind(Default = 5)]
                    public partial int RetryCount { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesBoolDefault()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind(Default = true)]
                    public partial bool IsEnabled { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesEnumDefault()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public enum Mode { Off, On, Auto }

                public partial class AppSettings
                {
                    [Bind(Default = Mode.Auto)]
                    public partial Mode Setting { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesIntDefaultOnLongProperty()
    {
        // Int literal is implicitly convertible to long — the generator should accept it and emit a cast.
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind(Default = 0)]
                    public partial long Counter { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task GeneratesDefaultWithStoreKeyAndKey()
    {
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind("secure", Key = "auth-token", Default = "anonymous")]
                    public partial string Token { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task ReportsDiagnosticWhenDefaultTypeMismatches()
    {
        // Assigning a string default to an int property is not a valid implicit conversion;
        // the generator must emit DI002 and skip code generation for that property.
        var source = """
            using Shiny;

            namespace TestNamespace
            {
                public partial class AppSettings
                {
                    [Bind(Default = "not-a-number")]
                    public partial int RetryCount { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }


    [Fact]
    public Task SkipsNotifyWhenClassAlreadyImplementsINPC()
    {
        var source = """
            using Shiny;
            using System.ComponentModel;

            namespace TestNamespace
            {
                [BindNotify]
                public partial class AppSettings : INotifyPropertyChanged
                {
                    public event PropertyChangedEventHandler? PropertyChanged;

                    [Bind]
                    public partial string Theme { get; set; }
                }
            }
            """;
        return TestHelper.VerifyDI(source);
    }
}
