using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Extensions.DependencyInjection.SourceGenerators;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shiny.Extensions.DependencyInjection.Tests;

public static class TestHelper
{
    public static Task Verify(string source, Dictionary<string, string>? msBuildProperties = null, string? assemblyName = null)
    {
        // Parse the source code
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Get all necessary references
        var references = GetMetadataReferences();

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName ?? "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create the source generator
        var generator = new DependencyInjectionSourceGenerator();

        // Create generator driver
        var driver = CSharpGeneratorDriver.Create(generator);

        // Configure MSBuild properties if provided
        if (msBuildProperties != null)
        {
            // Convert the properties to use the build_property. prefix that the source generator expects
            var analyzerProperties = new Dictionary<string, string>();
            foreach (var kvp in msBuildProperties)
            {
                analyzerProperties[$"build_property.{kvp.Key}"] = kvp.Value;
            }
            
            var analyzerConfigOptions = new TestAnalyzerConfigOptionsProvider(analyzerProperties);
            driver = (CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(analyzerConfigOptions);
        }

        // Run the generator
        var runResult = driver.RunGenerators(compilation);
        
        // Return verification of the generated sources
        return Verifier.Verify(runResult).UseDirectory("Snapshots");
    }

    private static List<MetadataReference> GetMetadataReferences()
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

        // Add Shiny.Extensions.DependencyInjection
        try
        {
            var shinyAssembly = typeof(ServiceAttribute).Assembly;
            references.Add(MetadataReference.CreateFromFile(shinyAssembly.Location));
        }
        catch
        {
            // Shiny assembly not available
        }

        return references;
    }
}

public class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
{
    public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(globalOptions);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => 
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => 
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());
}

public class TestAnalyzerConfigOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, out string value)
        => options.TryGetValue(key, out value!);
}
