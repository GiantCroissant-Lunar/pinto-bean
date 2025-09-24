// Tier-2: Source generators and analyzers for Yokan PintoBean service platform

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Yokan.PintoBean.CodeGen;

/// <summary>
/// Analyzer for Yokan PintoBean service platform guardrails and validation.
/// Implements diagnostic rules SG0001-SG0005 per RFC-0002.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PintoBeanAnalyzer : DiagnosticAnalyzer
{
    #region Diagnostic Descriptors

    /// <summary>
    /// SG0001: RealizeServiceAttribute used in Tier-1 -> error
    /// </summary>
    public static readonly DiagnosticDescriptor RealizeServiceInTier1 = new DiagnosticDescriptor(
        id: "SG0001",
        title: "RealizeServiceAttribute not allowed in Tier-1",
        messageFormat: "RealizeServiceAttribute is only allowed in Tier-2 (Generated Façades), Tier-3 (Adapters), and Tier-4 (Providers). Remove this attribute from Tier-1 (Contracts/Models).",
        category: "PintoBean.Tier",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The RealizeServiceAttribute should not be used in Tier-1 projects containing contracts and models. It is only valid in implementation tiers.");

    /// <summary>
    /// SG0002: RealizeService without contracts -> error
    /// </summary>
    public static readonly DiagnosticDescriptor RealizeServiceWithoutContracts = new DiagnosticDescriptor(
        id: "SG0002",
        title: "RealizeService without contracts",
        messageFormat: "RealizeServiceAttribute must specify at least one contract. This is likely a misconfiguration.",
        category: "PintoBean.Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The RealizeServiceAttribute constructor requires at least one contract type to be specified.");

    /// <summary>
    /// SG0003: Missing GenerateRegistry for realized contract -> error
    /// </summary>
    public static readonly DiagnosticDescriptor MissingGenerateRegistry = new DiagnosticDescriptor(
        id: "SG0003",
        title: "Missing GenerateRegistry for realized contract",
        messageFormat: "Contract '{0}' is realized but missing GenerateRegistryAttribute. Add [GenerateRegistry(typeof({0}))] to enable typed registry generation.",
        category: "PintoBean.Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each contract realized through RealizeServiceAttribute must have a corresponding GenerateRegistryAttribute to generate the required registry scaffolding.",
        customTags: new[] { "CompilationEnd" });

    /// <summary>
    /// SG0004: Façade method signature mismatch with contract -> error
    /// </summary>
    public static readonly DiagnosticDescriptor FacadeMethodSignatureMismatch = new DiagnosticDescriptor(
        id: "SG0004",
        title: "Façade method signature mismatch with contract",
        messageFormat: "Method '{0}' in façade class does not match the signature defined in contract '{1}'. Ensure all contract methods are properly implemented.",
        category: "PintoBean.Contract",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Façade methods must exactly match the signatures of their corresponding contract methods.");

    /// <summary>
    /// SG0005: Multi-contract façade spans categories -> warning
    /// </summary>
    public static readonly DiagnosticDescriptor MultiContractFacadeSpansCategories = new DiagnosticDescriptor(
        id: "SG0005",
        title: "Multi-contract façade spans categories",
        messageFormat: "Façade class realizes contracts from different categories: {0}. Consider splitting into separate façades to encourage cohesion.",
        category: "PintoBean.Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Multi-contract façades should ideally realize contracts from the same category to maintain good separation of concerns.");

    #endregion

    /// <summary>
    /// Gets the supported diagnostic descriptors for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            RealizeServiceInTier1,
            RealizeServiceWithoutContracts,
            MissingGenerateRegistry,
            FacadeMethodSignatureMismatch,
            MultiContractFacadeSpansCategories);

    /// <summary>
    /// Initializes the analyzer by registering analysis actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol == null) return;

        // Check for RealizeServiceAttribute
        var realizeServiceAttribute = GetRealizeServiceAttribute(classSymbol);
        if (realizeServiceAttribute != null)
        {
            AnalyzeRealizeServiceUsage(context, classDeclaration, classSymbol, realizeServiceAttribute);
        }
    }

    private static AttributeData? GetRealizeServiceAttribute(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes()
            .FirstOrDefault(attr => 
            {
                var attributeClass = attr.AttributeClass;
                return attributeClass?.Name == "RealizeServiceAttribute" &&
                       attributeClass.ContainingNamespace?.ToDisplayString() == "Yokan.PintoBean.CodeGen";
            });
    }

    private static void AnalyzeRealizeServiceUsage(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol classSymbol,
        AttributeData realizeServiceAttribute)
    {
        // SG0001: Check if RealizeServiceAttribute is used in Tier-1
        if (IsTier1Project(context.SemanticModel.Compilation))
        {
            var diagnostic = Diagnostic.Create(
                RealizeServiceInTier1,
                GetAttributeLocation(classDeclaration, "RealizeServiceAttribute") ?? classDeclaration.Identifier.GetLocation());
            context.ReportDiagnostic(diagnostic);
            return; // Don't proceed with other checks if this is Tier-1
        }

        // SG0002: Check if RealizeService has zero contracts
        var contractTypes = GetContractTypesFromAttribute(realizeServiceAttribute);
        if (contractTypes.Length == 0)
        {
            var diagnostic = Diagnostic.Create(
                RealizeServiceWithoutContracts,
                GetAttributeLocation(classDeclaration, "RealizeServiceAttribute") ?? classDeclaration.Identifier.GetLocation());
            context.ReportDiagnostic(diagnostic);
            return; // No point continuing if there are no contracts
        }

        // SG0004: Check façade method signature mismatches (simplified check)
        AnalyzeFacadeMethodSignatures(context, classSymbol, contractTypes);

        // SG0005: Check if multi-contract façade spans categories
        if (contractTypes.Length > 1)
        {
            AnalyzeMultiContractCategories(context, classDeclaration, contractTypes);
        }

        // SG0003: Check for missing GenerateRegistry for each realized contract
        foreach (var contractType in contractTypes)
        {
            if (!HasGenerateRegistryAttribute(context.SemanticModel.Compilation, contractType))
            {
                var diagnostic = Diagnostic.Create(
                    MissingGenerateRegistry,
                    GetAttributeLocation(classDeclaration, "RealizeServiceAttribute") ?? classDeclaration.Identifier.GetLocation(),
                    contractType.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool HasGenerateRegistryAttribute(Compilation compilation, ITypeSymbol contractType)
    {
        // Simple check - look for GenerateRegistry attribute on the contract type itself
        if (contractType is INamedTypeSymbol namedContract)
        {
            return namedContract.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "GenerateRegistryAttribute" &&
                            attr.AttributeClass.ContainingNamespace?.ToDisplayString() == "Yokan.PintoBean.CodeGen");
        }
        return false;
    }

    private static void AnalyzeFacadeMethodSignatures(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol classSymbol,
        ImmutableArray<ITypeSymbol> contractTypes)
    {
        // This is a simplified check - in a full implementation, we'd need to verify
        // that all contract methods are properly implemented in the façade
        foreach (var contractType in contractTypes)
        {
            if (contractType is INamedTypeSymbol namedContract)
            {
                var contractMethods = namedContract.GetMembers().OfType<IMethodSymbol>()
                    .Where(m => m.MethodKind == MethodKind.Ordinary && m.DeclaredAccessibility == Accessibility.Public);

                foreach (var contractMethod in contractMethods)
                {
                    var facadeMethod = classSymbol.GetMembers(contractMethod.Name).OfType<IMethodSymbol>()
                        .FirstOrDefault();

                    if (facadeMethod == null)
                    {
                        // Method not found - this will be caught by the source generator, but we can warn here too
                        var diagnostic = Diagnostic.Create(
                            FacadeMethodSignatureMismatch,
                            classSymbol.Locations.FirstOrDefault() ?? Location.None,
                            contractMethod.Name,
                            namedContract.ToDisplayString());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private static void AnalyzeMultiContractCategories(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<ITypeSymbol> contractTypes)
    {
        // Simplified category detection - in practice, this would be more sophisticated
        var categories = contractTypes.Select(GetContractCategory).Distinct().ToArray();
        
        if (categories.Length > 1)
        {
            var categoriesString = string.Join(", ", categories);
            var diagnostic = Diagnostic.Create(
                MultiContractFacadeSpansCategories,
                classDeclaration.Identifier.GetLocation(),
                categoriesString);
            context.ReportDiagnostic(diagnostic);
        }
    }

    #region Helper Methods

    private static bool IsTier1Project(Compilation compilation)
    {
        // Heuristic: Tier-1 projects typically have "Contracts", "Models", or "Abstractions" in their name
        // and don't reference Runtime or other implementation assemblies
        var assemblyName = compilation.AssemblyName?.ToLowerInvariant() ?? "";
        
        return assemblyName.Contains("contracts") || 
               assemblyName.Contains("models") || 
               assemblyName.Contains("abstractions");
    }

    private static Location? GetAttributeLocation(ClassDeclarationSyntax classDeclaration, string attributeName)
    {
        var attributeList = classDeclaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attr => attr.Name.ToString().Contains("RealizeService"));

        return attributeList?.GetLocation();
    }

    private static ImmutableArray<ITypeSymbol> GetContractTypesFromAttribute(AttributeData attribute)
    {
        var contractTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();

        // The params Type[] constructor can be represented in two ways:
        // 1. Single argument when one type is passed: [RealizeService(typeof(IService))]
        // 2. Array argument when multiple types are passed: [RealizeService(typeof(IService1), typeof(IService2))]
        
        if (attribute.ConstructorArguments.Length == 0)
        {
            return contractTypes.ToImmutable();
        }

        // Check if the first argument is an array (multiple types)
        var firstArgument = attribute.ConstructorArguments[0];
        if (firstArgument.Kind == TypedConstantKind.Array)
        {
            foreach (var element in firstArgument.Values)
            {
                if (element.Kind == TypedConstantKind.Type && element.Value is ITypeSymbol typeSymbol)
                {
                    contractTypes.Add(typeSymbol);
                }
            }
        }
        else
        {
            // Single arguments or multiple individual arguments (params expansion)
            foreach (var argument in attribute.ConstructorArguments)
            {
                if (argument.Kind == TypedConstantKind.Type && argument.Value is ITypeSymbol typeSymbol)
                {
                    contractTypes.Add(typeSymbol);
                }
            }
        }

        return contractTypes.ToImmutable();
    }

    private static string GetContractCategory(ITypeSymbol contractType)
    {
        // Simplified category detection based on namespace or naming patterns
        var namespaceParts = contractType.ContainingNamespace?.ToDisplayString().Split('.') ?? new string[0];
        
        // Look for common category indicators in the namespace
        foreach (var part in namespaceParts)
        {
            var lowerPart = part.ToLowerInvariant();
            if (lowerPart.Contains("user") || lowerPart.Contains("account") || lowerPart.Contains("auth"))
                return "User Management";
            if (lowerPart.Contains("order") || lowerPart.Contains("payment") || lowerPart.Contains("billing"))
                return "Commerce";
            if (lowerPart.Contains("inventory") || lowerPart.Contains("product") || lowerPart.Contains("catalog"))
                return "Inventory";
            if (lowerPart.Contains("notification") || lowerPart.Contains("messaging") || lowerPart.Contains("email"))
                return "Communication";
            if (lowerPart.Contains("report") || lowerPart.Contains("analytics") || lowerPart.Contains("metric"))
                return "Analytics";
        }

        // Fallback to interface name analysis
        var typeName = contractType.Name.ToLowerInvariant();
        if (typeName.Contains("user") || typeName.Contains("account") || typeName.Contains("auth"))
            return "User Management";
        if (typeName.Contains("order") || typeName.Contains("payment") || typeName.Contains("billing"))
            return "Commerce";
        if (typeName.Contains("inventory") || typeName.Contains("product") || typeName.Contains("catalog"))
            return "Inventory";
        if (typeName.Contains("notification") || typeName.Contains("messaging") || typeName.Contains("email"))
            return "Communication";
        if (typeName.Contains("report") || typeName.Contains("analytics") || typeName.Contains("metric"))
            return "Analytics";

        return "General";
    }

    #endregion
}