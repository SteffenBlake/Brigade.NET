using System;
using System.Collections.Immutable;
using System.Linq;
using Brigade.Net.Partie.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brigade.Net.Partie.Engines.AspNetCore;

internal static class HttpRouteAttributes
{
    private const string AttributeNamespace = "Brigade.Net.Partie.Engines.AspNetCore";

    public static RouteDeclaration? Discover(AttributeData attribute)
    {
        if (attribute.AttributeClass?.BaseType is { } baseType
            && baseType.OriginalDefinition.ToDisplayString() == AttributeNamespace + ".HandlerRouteAttribute<THandler>")
        {
            var pathIndex = attribute.AttributeConstructor?.Parameters.FirstOrDefault(parameter =>
                !parameter.GetAttributes().Any(marker => marker.AttributeClass?.ToDisplayString()
                    == "Brigade.Net.Partie.ParameterAttribute"))?.Ordinal ?? -1;
            return new RouteDeclaration(
                new[] { pathIndex < 0 || pathIndex >= attribute.ConstructorArguments.Length
                    ? "" : attribute.ConstructorArguments[pathIndex].Value as string ?? "" },
                attribute.AttributeClass.Name.Replace("Attribute", "").ToUpperInvariant(),
                (INamedTypeSymbol)baseType.TypeArguments[0],
                ContextParameters.Arguments(attribute)
            );
        }
        return null;
    }

    public static ImmutableArray<RoutePolicyEmission> DiscoverPolicies(
        IMethodSymbol method,
        string operation,
        RequestEmission request,
        Compilation compilation,
        Action<ISymbol, string> report
    )
    {
        var policies = ImmutableArray.CreateBuilder<RoutePolicyEmission>();
        var attributes = RouteGroupHierarchy.GetAttributes(method);
        foreach (var attribute in attributes.Where(attribute => IsPolicy(attribute)))
        {
            var generated = GeneratedPolicyType(attribute);
            var policyType = generated
                ?? attribute.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
            if (policyType is null)
            {
                report(method, "RoutePolicy must name a static, closed policy type");
                continue;
            }

            var isGenerated = generated is not null;
            var matchedPolicy = isGenerated
                ? MatchPolicy(policyType, operation, request, compilation)
                : policyType;
            if (isGenerated && matchedPolicy is null)
            {
                continue;
            }

            var methodNames = GetPolicyMethodNames(
                matchedPolicy!,
                operation,
                compilation,
                isGenerated
            );
            if (methodNames.Length != 1)
            {
                var signature = operation.Equals("GET", StringComparison.OrdinalIgnoreCase)
                    ? isGenerated ? "Query" : "Query<TParams>"
                    : isGenerated ? "Command" : "Command<TParams, TBody>";
                report(method, $"RoutePolicy '{policyType.ToDisplayString()}' must declare exactly one accessible, non-async static void {signature}(RouteHandlerBuilder route) method");
                continue;
            }
            var policyTypeName = matchedPolicy!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            policies.Add(new RoutePolicyEmission(
                policyTypeName,
                methodNames[0],
                !isGenerated
            ));
        }

        return policies.ToImmutable();
    }

    public static bool DiscoverPolicyFunctions(IMethodSymbol method) => method.Parameters.Length == 1
        && method.Parameters[0].RefKind == RefKind.None
        && method.Parameters[0].Type.ToDisplayString() == "Microsoft.AspNetCore.Builder.RouteHandlerBuilder";

    private static bool IsPolicy(AttributeData attribute) =>
        attribute.AttributeClass?.ToDisplayString() == AttributeNamespace + ".RoutePolicyAttribute"
        || GeneratedPolicyType(attribute) is not null;

    private static INamedTypeSymbol? GeneratedPolicyType(AttributeData attribute)
    {
        var marker = attribute.AttributeClass?.GetAttributes().FirstOrDefault(candidate =>
            candidate.AttributeClass?.ToDisplayString() == AttributeNamespace + ".RoutePolicyAttribute");
        return marker?.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
    }

    private static string[] GetPolicyMethodNames(
        INamedTypeSymbol policyType,
        string operation,
        Compilation compilation,
        bool implementsContract
    )
    {
        if ((!implementsContract && !policyType.IsStatic) || policyType.IsUnboundGenericType)
        {
            return Array.Empty<string>();
        }

        var isQuery = operation.Equals("GET", StringComparison.OrdinalIgnoreCase);
        return policyType.GetMembers(isQuery ? "Query" : "Command").OfType<IMethodSymbol>()
            .Where(candidate => candidate.IsStatic && !candidate.IsAsync && candidate.ReturnsVoid
                && candidate.Arity == (implementsContract ? 0 : isQuery ? 1 : 2)
                && candidate.Parameters.Length == 1
                && candidate.Parameters[0].RefKind == RefKind.None
                && candidate.Parameters[0].Type.ToDisplayString()
                    == "Microsoft.AspNetCore.Builder.RouteHandlerBuilder"
                && candidate.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .Select(candidate => candidate.Name).ToArray();
    }

    private static INamedTypeSymbol? MatchPolicy(
        INamedTypeSymbol policyType,
        string operation,
        RequestEmission request,
        Compilation compilation
    )
    {
        policyType = policyType.OriginalDefinition;
        var isQuery = operation.Equals("GET", StringComparison.OrdinalIgnoreCase);
        var metadataName = isQuery
            ? AttributeNamespace + ".IQueryRoutePolicy`1"
            : AttributeNamespace + ".ICommandRoutePolicy`1";
        var definition = compilation.GetTypeByMetadataName(metadataName);
        var contract = policyType.AllInterfaces.SingleOrDefault(candidate =>
            SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, definition));
        if (contract is null)
        {
            return null;
        }

        var prepared = PreparePolicyRequest(request, compilation);
        ITypeSymbol[] desired = [prepared.Request];
        var bindings = new System.Collections.Generic.Dictionary<ITypeParameterSymbol, ITypeSymbol>(
            SymbolEqualityComparer.Default
        );
        for (var index = 0; index < desired.Length; index++)
        {
            if (!Unify(contract.TypeArguments[index], desired[index], bindings))
            {
                return null;
            }
        }

        var definitionType = policyType.OriginalDefinition;
        if (definitionType.TypeParameters.Any(parameter => !bindings.ContainsKey(parameter)))
        {
            return null;
        }

        var closed = definitionType.Arity == 0
            ? definitionType
            : definitionType.Construct(definitionType.TypeParameters.Select(parameter => bindings[parameter]).ToArray());
        return HasValidConstraints(closed, prepared.Compilation) ? closed : null;
    }

    private static (Compilation Compilation, ITypeSymbol Request) PreparePolicyRequest(
        RequestEmission request,
        Compilation compilation
    )
    {
        var className = "__BrigadeRoutePolicyParams";
        while (compilation.GetTypeByMetadataName(className + "Types") is not null)
        {
            className += "_";
        }

        var tree = CSharpSyntaxTree.ParseText(
            "internal static class " + className + "Types { internal static " + request.TypeName
            + " Request = default!; }",
            compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions
        );
        var prepared = compilation.AddSyntaxTrees(tree);
        var types = prepared.GetTypeByMetadataName(className + "Types")!;
        return (prepared, ((IFieldSymbol)types.GetMembers("Request").Single()).Type);
    }

    private static bool HasValidConstraints(INamedTypeSymbol type, Compilation compilation)
    {
        var tree = CSharpSyntaxTree.ParseText(
            "internal static class __BrigadeRoutePolicyConstraintCheck { private static void Check()"
            + " { _ = typeof(" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "); } }",
            compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions
        );
        return !compilation.AddSyntaxTrees(tree).GetSemanticModel(tree).GetDiagnostics()
            .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    private static bool Unify(
        ITypeSymbol pattern,
        ITypeSymbol actual,
        System.Collections.Generic.Dictionary<ITypeParameterSymbol, ITypeSymbol> bindings
    )
    {
        if (pattern is ITypeParameterSymbol parameter)
        {
            if (bindings.TryGetValue(parameter, out var existing))
            {
                return SymbolEqualityComparer.Default.Equals(existing, actual);
            }

            bindings.Add(parameter, actual);
            return true;
        }

        if (pattern is IArrayTypeSymbol patternArray && actual is IArrayTypeSymbol actualArray)
        {
            return patternArray.Rank == actualArray.Rank
                && Unify(patternArray.ElementType, actualArray.ElementType, bindings);
        }

        if (pattern is not INamedTypeSymbol patternNamed || actual is not INamedTypeSymbol actualNamed
            || !SymbolEqualityComparer.Default.Equals(patternNamed.OriginalDefinition, actualNamed.OriginalDefinition)
            || patternNamed.TypeArguments.Length != actualNamed.TypeArguments.Length)
        {
            return SymbolEqualityComparer.Default.Equals(pattern, actual);
        }

        for (var index = 0; index < patternNamed.TypeArguments.Length; index++)
        {
            if (!Unify(patternNamed.TypeArguments[index], actualNamed.TypeArguments[index], bindings))
            {
                return false;
            }
        }

        return true;
    }

}
