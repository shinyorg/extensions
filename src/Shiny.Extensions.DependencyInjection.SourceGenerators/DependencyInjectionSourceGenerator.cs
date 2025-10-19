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

                // Check if the attribute is ServiceAttribute or inherits from it
                if (IsServiceAttributeOrDerived(attributeContainingTypeSymbol))
                {
                    return ExtractServiceInfo(context, typeDeclaration, attribute, attributeContainingTypeSymbol);
                }
            }
        }

        return null;
    }

    static bool IsServiceAttributeOrDerived(INamedTypeSymbol attributeType)
    {
        var current = attributeType;
        while (current != null)
        {
            if (current.ToDisplayString() == "Shiny.Extensions.DependencyInjection.ServiceAttribute")
                return true;
            current = current.BaseType;
        }
        return false;
    }

    static ServiceInfo? ExtractServiceInfo(GeneratorSyntaxContext context, TypeDeclarationSyntax typeDeclaration, AttributeSyntax attribute, INamedTypeSymbol attributeContainingTypeSymbol)
    {
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
        if (typeSymbol is null) return null;

        var lifetime = "Singleton"; // default
        string? keyedName = null;
        string? category = null;
        bool isTryAdd = false;
        bool asSelf = false;
        string? specificType = null;
        Location? attributeLocation = null;

        // Determine lifetime based on the specific attribute type
        var attributeTypeName = attributeContainingTypeSymbol.ToDisplayString();
        lifetime = attributeTypeName switch
        {
            "Shiny.Extensions.DependencyInjection.SingletonAttribute" => "Singleton",
            "Shiny.Extensions.DependencyInjection.ScopedAttribute" => "Scoped",
            "Shiny.Extensions.DependencyInjection.TransientAttribute" => "Transient",
            _ => "Singleton" // Default for base ServiceAttribute
        };

        // Store the attribute location for potential diagnostic reporting
        attributeLocation = attribute.GetLocation();

        // Find the matching attribute data (could be ServiceAttribute or any derived attribute)
        var attributeData = typeSymbol
            .GetAttributes()
            .FirstOrDefault(ad => 
                ad.AttributeClass != null && 
                IsServiceAttributeOrDerived(ad.AttributeClass)
            );

        if (attributeData != null)
        {
            // For base ServiceAttribute, extract lifetime from constructor argument
            if (attributeTypeName == "Shiny.Extensions.DependencyInjection.ServiceAttribute" && 
                attributeData.ConstructorArguments.Length > 0)
            {
                var lifetimeValue = attributeData.ConstructorArguments[0].Value;
                if (lifetimeValue is int enumValue)
                {
                    lifetime = enumValue switch
                    {
                        1 => "Scoped", 
                        2 => "Transient",
                        _ => "Singleton"
                    };
                }
            }

            // Extract named arguments (Type, KeyedName, Category, TryAdd, AsSelf)
            foreach (var namedArg in attributeData.NamedArguments)
            {
                if (namedArg is { Key: "Type", Value.Value: INamedTypeSymbol typeValue })
                {
                    specificType = typeValue.ToDisplayString();
                }
                else if (namedArg is { Key: "KeyedName", Value.Value: string keyedNameValue })
                {
                    keyedName = keyedNameValue;
                }
                else if (namedArg is { Key: "Category", Value.Value: string categoryValue })
                {
                    category = categoryValue;
                }
                else if (namedArg is { Key: "TryAdd", Value.Value: bool tryAddValue })
                {
                    isTryAdd = tryAddValue;
                }
                else if (namedArg is { Key: "AsSelf", Value.Value: bool asSelfValue })
                {
                    asSelf = asSelfValue;
                }
            }
        }
        else
        {
            // Fallback to old parsing logic for backward compatibility
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
            }
        }

        // Filter out system interfaces - only include user-defined interfaces
        var interfaces = typeSymbol.Interfaces
            .Where(i => !i.ToDisplayString().StartsWith("System."))
            .Select(i => i.ToDisplayString())
            .ToList();

        // If AsSelf is true, register the class directly (no interfaces)
        if (asSelf)
        {
            interfaces = [];
        }
        // If a specific type is specified, use only that type (if it's implemented by the service)
        else if (!string.IsNullOrEmpty(specificType))
        {
            // Check if the specific type is implemented by the service
            var implementsSpecificType = interfaces.Contains(specificType) || 
                                       typeSymbol.ToDisplayString() == specificType ||
                                       IsBaseClassOf(typeSymbol, specificType);
            
            if (implementsSpecificType)
            {
                // If it's the implementation type itself, register as implementation only
                if (typeSymbol.ToDisplayString() == specificType)
                {
                    interfaces = [];
                }
                else if (IsBaseClassOf(typeSymbol, specificType))
                {
                    // If the specific type is a base class, register as that base class only
                    interfaces = [specificType];
                }
                else
                {
                    // Use only the specified interface
                    interfaces = [specificType];
                }
            }
            else
            {
                // Specific type not found - this could be a configuration error
                // For now, we'll continue with all interfaces but could add a diagnostic
                // in the future
            }
        }

        var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();

        // Check if this is an open generic type (has type parameters)
        var isOpenGeneric = typeSymbol.IsGenericType;
        var genericArity = isOpenGeneric ? typeSymbol.Arity : 0;
        
        return new ServiceInfo
        {
            ClassName = typeSymbol.Name,
            TryAdd = isTryAdd,
            AsSelf = asSelf,
            FullClassName = typeSymbol.ToDisplayString(),
            Namespace = namespaceName,
            Lifetime = lifetime,
            KeyedName = keyedName,
            Category = category,
            Interfaces = interfaces,
            IsOpenGeneric = isOpenGeneric,
            GenericArity = genericArity,
            SpecificType = specificType,
            AttributeLocation = attributeLocation,
            HasConflictingConfiguration = asSelf && !string.IsNullOrEmpty(specificType)
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

        // Report diagnostics for services with conflicting configurations
        foreach (var service in validServices.Where(s => s.HasConflictingConfiguration))
        {
            var descriptor = new DiagnosticDescriptor(
                "DI001",
                "Conflicting service registration configuration",
                "Cannot specify both 'AsSelf = true' and 'Type' property on the same service attribute. Use either 'AsSelf = true' to register the implementation directly, or specify a 'Type' to register a specific interface.",
                "Usage",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var diagnostic = Diagnostic.Create(descriptor, service.AttributeLocation);
            context.ReportDiagnostic(diagnostic);
        }

        // Filter out services with conflicting configurations
        var validServicesForGeneration = validServices
            .Where(s => !s.HasConflictingConfiguration)
            .ToList();

        // Remove duplicates by creating a HashSet based on full class name
        var uniqueServices = validServicesForGeneration
            .GroupBy(s => s.FullClassName)
            .Select(g => g.First())
            .ToList();

        var source = GenerateRegistrationCode(uniqueServices);
        context.AddSource("GeneratedRegistrations.g.cs", source);
    }

    static string GenerateRegistrationCode(
        List<ServiceInfo> services
    )
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using global::System.Linq;");
        sb.AppendLine("using global::Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using global::Microsoft.Extensions.DependencyInjection.Extensions;");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("internal static class __ShinyServicesModule");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void Run()");
        sb.AppendLine("    {");
        sb.AppendLine("        global::Shiny.Extensions.DependencyInjection.Internals.ServiceRegistry.RegisterCallback((services, categories) => {");

        foreach (var service in services)
            GenerateServiceRegistration(sb, service);

        sb.AppendLine("        });");
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

        // If service has a category, wrap the registration in a conditional check
        if (!string.IsNullOrEmpty(service.Category))
        {
            sb.AppendLine($"            if (categories?.Any(x => x.Equals(\"{service.Category}\", global::System.StringComparison.OrdinalIgnoreCase)) == true)");
            sb.AppendLine("            {");
        }

        var indent = !string.IsNullOrEmpty(service.Category) ? "    " : "";
        var tryAdd = service.TryAdd ? "Try" : "";

        if (service.IsOpenGeneric)
        {
            // Convert full generic type names to open generic syntax for typeof()
            var openGenericClassName = ConvertToOpenGenericSyntax(service.FullClassName, service.GenericArity);
            var openGenericInterfaces = service.Interfaces
                .Select(i => ConvertToOpenGenericSyntax(i, GetGenericArityFromTypeName(i)))
                .ToList();
            
            // Open generic registration using typeof()
            if (service.KeyedName != null)
            {
                // Keyed open generic registration
                if (service.Interfaces.Count == 0)
                {
                    // Implementation only
                    sb.AppendLine($"            {indent}services.{tryAdd}AddKeyed{lifetimeMethod}(typeof(global::{openGenericClassName}), \"{service.KeyedName}\");");
                }
                else if (service.Interfaces.Count == 1)
                {
                    // Single interface
                    sb.AppendLine($"            {indent}services.{tryAdd}AddKeyed{lifetimeMethod}(typeof(global::{openGenericInterfaces[0]}), typeof(global::{openGenericClassName}), \"{service.KeyedName}\");");
                }
                else
                {
                    // Multiple interfaces - register as implementation only for keyed services
                    sb.AppendLine($"            {indent}services.{tryAdd}AddKeyed{lifetimeMethod}(typeof(global::{openGenericClassName}), \"{service.KeyedName}\");");
                }
            }
            else
            {
                // Non-keyed open generic registration
                if (service.Interfaces.Count == 0)
                {
                    // Implementation only
                    sb.AppendLine($"            {indent}services.{tryAdd}Add{lifetimeMethod}(typeof(global::{openGenericClassName}));");
                }
                else if (service.Interfaces.Count == 1)
                {
                    // Single interface
                    sb.AppendLine($"            {indent}services.{tryAdd}Add{lifetimeMethod}(typeof(global::{openGenericInterfaces[0]}), typeof(global::{openGenericClassName}));");
                }
                else
                {
                    // Multiple interfaces - register for each interface
                    for (int i = 0; i < service.Interfaces.Count; i++)
                    {
                        sb.AppendLine($"            {indent}services.{tryAdd}Add{lifetimeMethod}(typeof(global::{openGenericInterfaces[i]}), typeof(global::{openGenericClassName}));");
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
                    sb.AppendLine($"            {indent}services.{tryAdd}AddKeyed{lifetimeMethod}<global::{service.FullClassName}>(\"{service.KeyedName}\");");
                }
                else if (service.Interfaces.Count == 1)
                {
                    // Single interface
                    sb.AppendLine($"            {indent}services.{tryAdd}AddKeyed{lifetimeMethod}<global::{service.Interfaces[0]}, global::{service.FullClassName}>(\"{service.KeyedName}\");");
                }
                else
                {
                    // Multiple interfaces
                    // TODO: this will fail for transient
                    sb.AppendLine($"            {indent}global::Shiny.Extensions.DependencyInjection.ServiceCollectionExtensions.Add{lifetimeMethod}AsImplementedInterfaces<global::{service.FullClassName}>(services, \"{service.KeyedName}\");");
                }
            }
            else
            {
                // Non-keyed registration
                if (service.Interfaces.Count == 0)
                {
                    // Implementation only
                    sb.AppendLine($"            {indent}services.{tryAdd}Add{lifetimeMethod}<global::{service.FullClassName}>();");
                }
                else if (service.Interfaces.Count == 1)
                {
                    // Single interface
                    sb.AppendLine($"            {indent}services.{tryAdd}Add{lifetimeMethod}<global::{service.Interfaces[0]}, global::{service.FullClassName}>();");
                }
                else
                {
                    // Multiple interfaces
                    // TODO: this will fail for transient
                    sb.AppendLine($"            {indent}global::Shiny.Extensions.DependencyInjection.ServiceCollectionExtensions.Add{lifetimeMethod}AsImplementedInterfaces<global::{service.FullClassName}>(services);");
                }
            }
        }

        // Close the category conditional block
        if (!string.IsNullOrEmpty(service.Category))
        {
            sb.AppendLine("            }");
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

    static bool IsBaseClassOf(INamedTypeSymbol derivedType, string baseTypeName)
    {
        // Check if any of the base types match the specified type name
        var current = derivedType.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == baseTypeName)
                return true;
            current = current.BaseType;
        }

        // Also check interfaces
        foreach (var iface in derivedType.AllInterfaces)
        {
            if (iface.ToDisplayString() == baseTypeName)
                return true;
        }

        return false;
    }
}

class ServiceInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string FullClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Lifetime { get; set; } = string.Empty;
    public string? KeyedName { get; set; }
    public string? Category { get; set; }
    public List<string> Interfaces { get; set; } = [];
    public bool IsOpenGeneric { get; set; } = false;
    public int GenericArity { get; set; } = 0;
    public bool TryAdd { get; set; }
    public bool AsSelf { get; set; }
    public string? SpecificType { get; set; }
    public Location? AttributeLocation { get; set; }
    public bool HasConflictingConfiguration { get; set; }
}