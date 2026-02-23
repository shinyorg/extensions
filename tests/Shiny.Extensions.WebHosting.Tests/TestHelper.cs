using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Extensions.WebHosting.SourceGenerators;
using System.Reflection;

namespace Shiny.Extensions.WebHosting.Tests;

public static class TestHelper
{
    public static Task VerifyWebHosting(string source, string? assemblyName = null, bool includeAspNetCore = true)
    {
        // Parse the source code
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Get all necessary references
        var references = GetMetadataReferences(includeAspNetCore);

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName ?? "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create generator driver with the specified generator
        var driver = CSharpGeneratorDriver.Create(new WebHostingSourceGenerator());

        // Run the generator
        var runResult = driver.RunGenerators(compilation);

        // Return verification of the generated sources
        return Verifier.Verify(runResult).UseDirectory("Snapshots");
    }

    private static List<MetadataReference> GetMetadataReferences(bool includeAspNetCore = true)
    {
        var references = new List<MetadataReference>();

        // Add basic .NET references
        var netAssemblies = new[]
        {
            typeof(object).Assembly,                    // System.Runtime
            typeof(Console).Assembly,                   // System.Console
            typeof(Attribute).Assembly,                 // System.Runtime
            Assembly.Load("System.Runtime"),
            Assembly.Load("System.Collections"),
            Assembly.Load("netstandard")
        };

        foreach (var assembly in netAssemblies)
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
            catch
            {
                // Skip if assembly can't be loaded
            }
        }

        // Add Microsoft.Extensions.DependencyInjection
        try
        {
            var diAssembly = typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly;
            references.Add(MetadataReference.CreateFromFile(diAssembly.Location));
        }
        catch
        {
            // DI assembly not available
        }

        // Add Shiny.Extensions.WebHosting (for IWebModule)
        try
        {
            var shinyAssembly = typeof(Shiny.IWebModule).Assembly;
            references.Add(MetadataReference.CreateFromFile(shinyAssembly.Location));
        }
        catch
        {
            // Shiny assembly not available
        }

        // Add ASP.NET Core references if requested
        if (includeAspNetCore)
        {
            try
            {
                // Load Microsoft.AspNetCore.App assemblies
                var aspNetCoreAssemblies = new[]
                {
                    "Microsoft.AspNetCore",
                    "Microsoft.AspNetCore.Http",
                    "Microsoft.AspNetCore.Http.Abstractions",
                    "Microsoft.AspNetCore.Routing",
                    "Microsoft.AspNetCore.Authentication",
                    "Microsoft.AspNetCore.Authentication.Abstractions",
                    "Microsoft.AspNetCore.Authorization",
                    "Microsoft.AspNetCore.Cors"
                };

                foreach (var assemblyName in aspNetCoreAssemblies)
                {
                    try
                    {
                        var assembly = Assembly.Load(assemblyName);
                        references.Add(MetadataReference.CreateFromFile(assembly.Location));
                    }
                    catch
                    {
                        // Skip if assembly can't be loaded
                    }
                }

                try
                {
                    var hostingAssembly = Assembly.Load("Microsoft.Extensions.Hosting");
                    references.Add(MetadataReference.CreateFromFile(hostingAssembly.Location));
                }
                catch
                {
                    // Skip if assembly can't be loaded
                }

                try
                {
                    var hostingAbstractionsAssembly = Assembly.Load("Microsoft.Extensions.Hosting.Abstractions");
                    references.Add(MetadataReference.CreateFromFile(hostingAbstractionsAssembly.Location));
                }
                catch
                {
                    // Skip if assembly can't be loaded
                }
            }
            catch
            {
                // ASP.NET Core assemblies not available
            }
        }

        return references;
    }
}
