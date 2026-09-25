using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

/// <summary>
/// Shared lexical group discovery for transport generators; unrelated containers add no scope.
/// </summary>
public static class RouteGroupHierarchy
{
    /// <summary>
    /// Gets the enclosing groups, outermost first, including the supplied type if it is a group.
    /// </summary>
    public static ImmutableArray<INamedTypeSymbol> GetGroups(INamedTypeSymbol type)
    {
        var groups = new Stack<INamedTypeSymbol>();
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.GetAttributes().Any(IsGroup))
            {
                groups.Push(current);
            }
        }

        return groups.ToImmutableArray();
    }

    /// <summary>Gets group attributes outermost first, followed by the route's own attributes.</summary>
    public static IEnumerable<AttributeData> GetAttributes(IMethodSymbol route) =>
        GetGroups(route.ContainingType)
            .SelectMany(group => group.GetAttributes())
            .Concat(route.GetAttributes());

    internal static bool IsGroup(AttributeData attribute) =>
        attribute.AttributeClass?.ToDisplayString() == "Brigade.Net.Partie.BrigadeGroupAttribute";
}
