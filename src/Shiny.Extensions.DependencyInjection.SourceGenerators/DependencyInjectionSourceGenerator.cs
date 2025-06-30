using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Shiny.Extensions.DependencyInjection.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class DependencyInjectionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find classes with Service attribute
        var classesWithServiceAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null)
            .Collect(); // Collect all results first

        // Combine with compilation for namespace resolution
        var compilationAndClasses = context.CompilationProvider.Combine(classesWithServiceAttribute);

        context.RegisterSourceOutput(
            compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc)
        );
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node) => 
        node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
    

    static ServiceInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                    continue;

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == "Shiny.Extensions.DependencyInjection.ServiceAttribute")
                {
                    return ExtractServiceInfo(context, classDeclaration, attribute);
                }
            }
        }

        return null;
    }

    static ServiceInfo? ExtractServiceInfo(GeneratorSyntaxContext context, ClassDeclarationSyntax classDeclaration, AttributeSyntax attribute)
    {
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        if (classSymbol is null) return null;

        var lifetime = "Singleton"; // default
        string? keyedName = null;

        if (attribute.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = attribute.ArgumentList.Arguments[0];
            if (context.SemanticModel.GetConstantValue(firstArg.Expression).HasValue)
            {
                var lifetimeValue = context.SemanticModel.GetConstantValue(firstArg.Expression).Value;
                // ServiceLifetime enum: Singleton = 0, Scoped = 1, Transient = 2
                if (lifetimeValue is int enumValue)
                {
                    lifetime = enumValue switch
                    {
                        0 => "Singleton",
                        1 => "Scoped", 
                        2 => "Transient",
                        _ => "Singleton"
                    };
                }
                else
                {
                    lifetime = lifetimeValue?.ToString() ?? "Singleton";
                }
            }
            else
            {
                // Handle ServiceLifetime enum values
                var expressionText = firstArg.Expression.ToString();
                if (expressionText.Contains("Singleton"))
                    lifetime = "Singleton";
                else if (expressionText.Contains("Transient"))
                    lifetime = "Transient";
                else if (expressionText.Contains("Scoped"))
                    lifetime = "Scoped";
            }

            if (attribute.ArgumentList.Arguments.Count > 1)
            {
                var secondArg = attribute.ArgumentList.Arguments[1];
                if (context.SemanticModel.GetConstantValue(secondArg.Expression).HasValue)
                {
                    keyedName = context.SemanticModel.GetConstantValue(secondArg.Expression).Value as string;
                }
            }
        }

        var interfaces = classSymbol.Interfaces.Select(i => i.ToDisplayString()).ToList();
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        return new ServiceInfo
        {
            ClassName = classSymbol.Name,
            FullClassName = classSymbol.ToDisplayString(),
            Namespace = namespaceName,
            Lifetime = lifetime,
            KeyedName = keyedName,
            Interfaces = interfaces
        };
    }

    static void Execute(Compilation compilation, ImmutableArray<ServiceInfo?> services, SourceProductionContext context)
    {
        if (services.IsDefaultOrEmpty)
            return;

        var validServices = services.Where(s => s != null).Cast<ServiceInfo>().ToList();
        if (validServices.Count == 0)
            return;

        // Remove duplicates by creating a HashSet based on full class name
        var uniqueServices = validServices
            .GroupBy(s => s.FullClassName)
            .Select(g => g.First())
            .ToList();

        // Group services by namespace and generate a single file for each namespace
        var servicesByNamespace = uniqueServices.GroupBy(s => s.Namespace);

        foreach (var namespaceGroup in servicesByNamespace)
        {
            var targetNamespace = GetTargetNamespace(compilation, namespaceGroup.Key);
            var servicesList = namespaceGroup.ToList();
            
            // Only generate if we have services for this namespace
            if (servicesList.Count > 0)
            {
                var source = GenerateRegistrationCode(targetNamespace, servicesList);
                
                // Use a deterministic, unique filename per namespace
                var safeNamespace = string.IsNullOrEmpty(targetNamespace) || targetNamespace == "<global namespace>" 
                    ? "Global" 
                    : targetNamespace.Replace(".", "_").Replace("<", "_").Replace(">", "_");
                var fileName = $"GeneratedRegistrations_{safeNamespace}.g.cs";
                context.AddSource(fileName, source);
            }
        }
    }

    static string GetTargetNamespace(Compilation compilation, string defaultNamespace)
    {
        // Look for ShinyExtensionsDependencyInjectionNamespace in project properties
        // For now, just use the default namespace from the services
        // In a real implementation, you'd parse the MSBuild properties
        _ = compilation; // Suppress unused parameter warning
        return defaultNamespace;
    }

    static string GenerateRegistrationCode(string namespaceName, List<ServiceInfo> services)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using global::Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using global::Shiny.Extensions.DependencyInjection;");
        sb.AppendLine();
        
        var isGlobalNamespace = string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>";
        
        if (!isGlobalNamespace)
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
        }

        // Use a consistent class name
        sb.AppendLine($"    public static class __GeneratedRegistrations");
        sb.AppendLine("    {");
        sb.AppendLine("        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddGeneratedServices(");
        sb.AppendLine("            this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services");
        sb.AppendLine("        )");
        sb.AppendLine("        {");

        foreach (var service in services)
        {
            GenerateServiceRegistration(sb, service);
        }

        sb.AppendLine();
        sb.AppendLine("            return services;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        if (!isGlobalNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    static void GenerateServiceRegistration(StringBuilder sb, ServiceInfo service)
    {
        var lifetimeMethod = service.Lifetime switch
        {
            "Transient" => "Transient",
            "Scoped" => "Scoped",
            _ => "Singleton"
        };

        if (service.KeyedName != null)
        {
            // Keyed registration
            if (service.Interfaces.Count == 0)
            {
                // Implementation only
                sb.AppendLine($"            services.AddKeyed{lifetimeMethod}<{service.ClassName}>(\"{service.KeyedName}\");");
            }
            else if (service.Interfaces.Count == 1)
            {
                // Single interface
                sb.AppendLine($"            services.AddKeyed{lifetimeMethod}<{service.Interfaces[0]}, {service.ClassName}>(\"{service.KeyedName}\");");
            }
            else
            {
                // Multiple interfaces - no keyed equivalent, so register as implementation only
                sb.AppendLine($"            services.AddKeyed{lifetimeMethod}<{service.ClassName}>(\"{service.KeyedName}\");");
            }
        }
        else
        {
            // Non-keyed registration
            if (service.Interfaces.Count == 0)
            {
                // Implementation only
                sb.AppendLine($"            services.Add{lifetimeMethod}<{service.ClassName}>();");
            }
            else if (service.Interfaces.Count == 1)
            {
                // Single interface
                sb.AppendLine($"            services.Add{lifetimeMethod}<{service.Interfaces[0]}, {service.ClassName}>();");
            }
            else
            {
                // Multiple interfaces
                sb.AppendLine($"            services.Add{lifetimeMethod}AsImplementedInterfaces<{service.ClassName}>();");
            }
        }
    }
}

class ServiceInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string FullClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Lifetime { get; set; } = string.Empty;
    public string? KeyedName { get; set; }
    public List<string> Interfaces { get; set; } = [];
}