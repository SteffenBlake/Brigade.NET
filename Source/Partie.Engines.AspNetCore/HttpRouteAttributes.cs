using System;
using System.Collections.Immutable;
using System.Linq;
using Brigade.Net.Partie.Generator;
using Microsoft.CodeAnalysis;

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

            var methodNames = GetPolicyMethodNames(policyType, operation, compilation, generated is not null);
            if (methodNames.Length != 1)
            {
                var signature = operation.Equals("GET", StringComparison.OrdinalIgnoreCase)
                    ? "Query<TParams>"
                    : "Command<TParams, TBody>";
                report(method, $"RoutePolicy '{policyType.ToDisplayString()}' must declare exactly one accessible, non-async static void {signature}(RouteHandlerBuilder route) method");
                continue;
            }
            policies.Add(new RoutePolicyEmission(
                policyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                methodNames[0]
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
        var builderType = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.RouteHandlerBuilder");
        return policyType.GetMembers(isQuery ? "Query" : "Command").OfType<IMethodSymbol>()
            .Where(candidate => candidate.IsStatic && !candidate.IsAsync && candidate.ReturnsVoid
                && candidate.Arity == (isQuery ? 1 : 2) && candidate.Parameters.Length == 1
                && candidate.Parameters[0].RefKind == RefKind.None
                && SymbolEqualityComparer.Default.Equals(candidate.Parameters[0].Type, builderType)
                && compilation.IsSymbolAccessibleWithin(candidate, compilation.Assembly))
            .Select(candidate => candidate.Name).ToArray();
    }

}
