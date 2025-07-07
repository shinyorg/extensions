using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text;

namespace Shiny.Extensions.DependencyInjection.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class DependencyInjectionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find classes and records with Service attribute
        var typesWithServiceAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null)
            .Collect();

        var configOptions = context.AnalyzerConfigOptionsProvider;

        var compilationAndTypesAndConfig = context.CompilationProvider
            .Combine(typesWithServiceAttribute)
            .Combine(configOptions);

        context.RegisterSourceOutput(
            compilationAndTypesAndConfig,
            static (spc, source) => Execute(source.Left.Left, source.Left.Right, source.Right, spc)
        );
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node) => 
        node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } or 
                RecordDeclarationSyntax { AttributeLists.Count: > 0 };
    

    static ServiceInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // Handle both class and record declarations
        TypeDeclarationSyntax typeDeclaration = context.Node switch
        {
            ClassDeclarationSyntax classDecl => classDecl,
            RecordDeclarationSyntax recordDecl => recordDecl,
            _ => throw new InvalidOperationException($"Unexpected node type: {context.Node.GetType()}")
        };
        
        foreach (var attributeList in typeDeclaration.AttributeLists)
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
                    return ExtractServiceInfo(context, typeDeclaration, attribute);
                }
            }
        }

        return null;
    }

    static ServiceInfo? ExtractServiceInfo(GeneratorSyntaxContext context, TypeDeclarationSyntax typeDeclaration, AttributeSyntax attribute)
    {
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
        if (typeSymbol is null) return null;

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

        // Filter out system interfaces - only include user-defined interfaces
        var interfaces = typeSymbol.Interfaces
            .Where(i => !i.ToDisplayString().StartsWith("System."))
            .Select(i => i.ToDisplayString())
            .ToList();
        var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();

        // Check if this is an open generic type (has type parameters)
        var isOpenGeneric = typeSymbol.IsGenericType;
        var genericArity = isOpenGeneric ? typeSymbol.Arity : 0;
        
        return new ServiceInfo
        {
            ClassName = typeSymbol.Name,
            FullClassName = typeSymbol.ToDisplayString(),
            Namespace = namespaceName,
            Lifetime = lifetime,
            KeyedName = keyedName,
            Interfaces = interfaces,
            IsOpenGeneric = isOpenGeneric,
            GenericArity = genericArity
        };
    }

    static void Execute(Compilation compilation, ImmutableArray<ServiceInfo?> services, AnalyzerConfigOptionsProvider configOptions, SourceProductionContext context)
    {
        // Always generate the extension class, even if there are no services
        var validServices = services.IsDefaultOrEmpty 
            ? []
            : services
                .Where(s => s != null)
                .Cast<ServiceInfo>()
                .ToList();

        // Remove duplicates by creating a HashSet based on full class name
        var uniqueServices = validServices
            .GroupBy(s => s.FullClassName)
            .Select(g => g.First())
            .ToList();

        // Generate a single extension class for all types in the assembly
        var targetNamespace = GetTargetNamespace(compilation, configOptions);
        var extensionMethodName = GetExtensionMethodName(configOptions);
        var useInternalAccessor = configOptions.GlobalOptions.TryGetValue("build_property.ShinyDIUseInternalAccessor", out var useInternal) && 
                                  Boolean.TryParse(useInternal, out _);
        var source = GenerateRegistrationCode(targetNamespace, uniqueServices, extensionMethodName, useInternalAccessor);
        
        context.AddSource("GeneratedRegistrations.g.cs", source);
    }

    static string GetExtensionMethodName(AnalyzerConfigOptionsProvider configOptions)
    {
        var method = "AddGeneratedServices";
        if (configOptions.GlobalOptions.TryGetValue("build_property.ShinyDIExtensionMethodName", out var methodName) && 
            !String.IsNullOrEmpty(methodName))
        {
            method = methodName;
        }

        return method;
    }

    static string GetTargetNamespace(Compilation compilation, AnalyzerConfigOptionsProvider configOptions)
    {
        // Try to get ShinyDIExtensionNamespace from MSBuild properties via analyzer config
        var globalOptions = configOptions.GlobalOptions;
        
        // Check for ShinyDIExtensionNamespace property
        if (globalOptions.TryGetValue("build_property.ShinyDIExtensionNamespace", out var shinyDINamespace) && 
            !string.IsNullOrEmpty(shinyDINamespace))
        {
            return shinyDINamespace;
        }

        // Fallback to RootNamespace property
        if (globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace) && 
            !string.IsNullOrEmpty(rootNamespace))
        {
            return rootNamespace;
        }

        // Fallback to assembly name
        var assemblyName = compilation.AssemblyName;
        if (!string.IsNullOrEmpty(assemblyName))
            return assemblyName;

        // Final fallback
        return "Generated";
    }

    static string GenerateRegistrationCode(string namespaceName, List<ServiceInfo> services, string extensionMethodName, bool useInternalAccessor)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using global::Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        
        var isGlobalNamespace = string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>";
        
        if (!isGlobalNamespace)
            sb.AppendLine($"namespace {namespaceName};").AppendLine();
        
        // Use a consistent class name
        var accessModifier = useInternalAccessor ? "internal" : "public";
        sb.AppendLine($"{accessModifier} static class __GeneratedRegistrations");
        sb.AppendLine("{");
        sb.AppendLine($"    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection {extensionMethodName}(");
        sb.AppendLine("        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services");
        sb.AppendLine("    )");
        sb.AppendLine("    {");

        foreach (var service in services)
            GenerateServiceRegistration(sb, service);

        sb.AppendLine();
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

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

        if (service.IsOpenGeneric)
        {
            // Convert full generic type names to open generic syntax for typeof()
            var openGenericClassName = ConvertToOpenGenericSyntax(service.FullClassName, service.GenericArity);
            var openGenericInterfaces = service.Interfaces.Select(i => ConvertToOpenGenericSyntax(i, GetGenericArityFromTypeName(i))).ToList();

            // Open generic registration using typeof()
            if (service.KeyedName != null)
            {
                // Keyed open generic registration
                if (service.Interfaces.Count == 0)
                {
                    // Implementation only
                    sb.AppendLine($"        services.AddKeyed{lifetimeMethod}(typeof(global::{openGenericClassName}), \"{service.KeyedName}\");");
                }
                else if (service.Interfaces.Count == 1)
                {
                    // Single interface
                    sb.AppendLine($"        services.AddKeyed{lifetimeMethod}(typeof(global::{openGenericInterfaces[0]}), typeof(global::{openGenericClassName}), \"{service.KeyedName}\");");
                }
                else
                {
                    // Multiple interfaces - register as implementation only for keyed services
                    sb.AppendLine($"        services.AddKeyed{lifetimeMethod}(typeof(global::{openGenericClassName}), \"{service.KeyedName}\");");
                }
            }
            else
            {
                // Non-keyed open generic registration
                if (service.Interfaces.Count == 0)
                {
                    // Implementation only
                    sb.AppendLine($"        services.Add{lifetimeMethod}(typeof(global::{openGenericClassName}));");
                }
                else if (service.Interfaces.Count == 1)
                {
                    // Single interface
                    sb.AppendLine($"        services.Add{lifetimeMethod}(typeof(global::{openGenericInterfaces[0]}), typeof(global::{openGenericClassName}));");
                }
                else
                {
                    // Multiple interfaces - register for each interface
                    for (int i = 0; i < service.Interfaces.Count; i++)
                    {
                        sb.AppendLine($"        services.Add{lifetimeMethod}(typeof(global::{openGenericInterfaces[i]}), typeof(global::{openGenericClassName}));");
                    }
                }
            }
        }
        else
        {
            // Closed type registration (existing logic)
            if (service.KeyedName != null)
            {
                // Keyed registration
                if (service.Interfaces.Count == 0)
                {
                    // Implementation only
                    sb.AppendLine($"        services.AddKeyed{lifetimeMethod}<global::{service.FullClassName}>(\"{service.KeyedName}\");");
                }
                else if (service.Interfaces.Count == 1)
                {
                    // Single interface
                    sb.AppendLine($"        services.AddKeyed{lifetimeMethod}<global::{service.Interfaces[0]}, global::{service.FullClassName}>(\"{service.KeyedName}\");");
                }
                else
                {
                    // Multiple interfaces
                    // TODO: this will fail for transient
                    sb.AppendLine($"        global::Shiny.Extensions.DependencyInjection.ServiceCollectionExtensions.Add{lifetimeMethod}AsImplementedInterfaces<global::{service.FullClassName}>(services, \"{service.KeyedName}\");");
                }
            }
            else
            {
                // Non-keyed registration
                if (service.Interfaces.Count == 0)
                {
                    // Implementation only
                    sb.AppendLine($"        services.Add{lifetimeMethod}<global::{service.FullClassName}>();");
                }
                else if (service.Interfaces.Count == 1)
                {
                    // Single interface
                    sb.AppendLine($"        services.Add{lifetimeMethod}<global::{service.Interfaces[0]}, global::{service.FullClassName}>();");
                }
                else
                {
                    // Multiple interfaces
                    // TODO: this will fail for transient
                    sb.AppendLine($"        global::Shiny.Extensions.DependencyInjection.ServiceCollectionExtensions.Add{lifetimeMethod}AsImplementedInterfaces<global::{service.FullClassName}>(services);");
                }
            }
        }
    }

    static string ConvertToOpenGenericSyntax(string fullTypeName, int genericArity)
    {
        if (genericArity <= 0)
            return fullTypeName;

        // Find the last occurrence of < to handle nested generic types correctly
        var lastAngleBracketIndex = fullTypeName.LastIndexOf('<');
        if (lastAngleBracketIndex == -1)
            return fullTypeName;

        var baseTypeName = fullTypeName.Substring(0, lastAngleBracketIndex);
        
        // Create the open generic syntax with the right number of commas
        var commas = genericArity > 1 ? new string(',', genericArity - 1) : "";
        return $"{baseTypeName}<{commas}>";
    }

    static int GetGenericArityFromTypeName(string typeName)
    {
        // Count the number of type parameters by counting commas + 1 within the generic brackets
        var lastAngleBracketIndex = typeName.LastIndexOf('<');
        if (lastAngleBracketIndex == -1)
            return 0;

        var closingBracketIndex = typeName.LastIndexOf('>');
        if (closingBracketIndex <= lastAngleBracketIndex)
            return 0;

        var genericPart = typeName.Substring(lastAngleBracketIndex + 1, closingBracketIndex - lastAngleBracketIndex - 1);
        
        if (string.IsNullOrWhiteSpace(genericPart))
            return 0;

        // Count commas and add 1 to get the number of type parameters
        var commaCount = genericPart.Count(c => c == ',');
        return commaCount + 1;
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
    public bool IsOpenGeneric { get; set; } = false;
    public int GenericArity { get; set; } = 0;
}