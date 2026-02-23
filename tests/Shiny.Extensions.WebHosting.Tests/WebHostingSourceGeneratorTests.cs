namespace Shiny.Extensions.WebHosting.Tests;


public class WebHostingSourceGeneratorTests
{
    [Fact]
    public Task DoesNotGenerateForNonAspNetCoreProject()
    {
        var source = """
            namespace TestNamespace
            {
                public class SomeService
                {
                    public string GetValue() => "Hello";
                }
            }
            """;

        return TestHelper.VerifyWebHosting(source, includeAspNetCore: false);
    }

    [Fact]
    public Task GeneratesExtensionsWithNoModules()
    {
        var source = """
            namespace TestNamespace
            {
                public class SomeService
                {
                    public string GetValue() => "Hello";
                }
            }
            """;

        return TestHelper.VerifyWebHosting(source);
    }

    [Fact]
    public Task GeneratesWithSingleModule()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public class CorsModule : Shiny.IWebModule
                {
                    public void Add(WebApplicationBuilder builder)
                    {
                        builder.Services.AddCors();
                    }

                    public void Use(WebApplication app)
                    {
                        app.UseCors();
                    }
                }
            }
            """;

        return TestHelper.VerifyWebHosting(source);
    }

    [Fact]
    public Task GeneratesWithMultipleModules()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public class CorsModule : Shiny.IWebModule
                {
                    public void Add(WebApplicationBuilder builder)
                    {
                        builder.Services.AddCors();
                    }

                    public void Use(WebApplication app)
                    {
                        app.UseCors();
                    }
                }

                public class AuthModule : Shiny.IWebModule
                {
                    public void Add(WebApplicationBuilder builder)
                    {
                        builder.Services.AddAuthentication();
                    }

                    public void Use(WebApplication app)
                    {
                        app.UseAuthentication();
                    }
                }
            }
            """;

        return TestHelper.VerifyWebHosting(source);
    }

    [Fact]
    public Task SkipsAbstractModules()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestNamespace
            {
                public abstract class BaseModule : Shiny.IWebModule
                {
                    public abstract void Add(WebApplicationBuilder builder);
                    public abstract void Use(WebApplication app);
                }

                public class ConcreteModule : Shiny.IWebModule
                {
                    public void Add(WebApplicationBuilder builder)
                    {
                        builder.Services.AddCors();
                    }

                    public void Use(WebApplication app)
                    {
                        app.UseCors();
                    }
                }
            }
            """;

        return TestHelper.VerifyWebHosting(source);
    }

    [Fact]
    public Task GeneratesWithModulesInDifferentNamespaces()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.DependencyInjection;

            namespace Infrastructure.Cors
            {
                public class CorsModule : Shiny.IWebModule
                {
                    public void Add(WebApplicationBuilder builder)
                    {
                        builder.Services.AddCors();
                    }

                    public void Use(WebApplication app)
                    {
                        app.UseCors();
                    }
                }
            }

            namespace Infrastructure.Auth
            {
                public class AuthModule : Shiny.IWebModule
                {
                    public void Add(WebApplicationBuilder builder)
                    {
                        builder.Services.AddAuthentication();
                    }

                    public void Use(WebApplication app)
                    {
                        app.UseAuthentication();
                    }
                }
            }
            """;

        return TestHelper.VerifyWebHosting(source);
    }

    [Fact]
    public Task GeneratesWithModuleUsingShortInterfaceName()
    {
        var source = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.DependencyInjection;
            using Shiny;

            namespace TestNamespace
            {
                public class CorsModule : IWebModule
                {
                    public void Add(WebApplicationBuilder builder)
                    {
                        builder.Services.AddCors();
                    }

                    public void Use(WebApplication app)
                    {
                        app.UseCors();
                    }
                }
            }
            """;

        return TestHelper.VerifyWebHosting(source);
    }
}
