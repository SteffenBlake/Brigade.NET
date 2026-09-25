using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteGraph(
    ITypeSymbol resultType,
    ImmutableArray<RouteCall> calls,
    ImmutableArray<RouteValue> externalValues
)
{
    public ITypeSymbol ResultType { get; } = resultType;

    public ImmutableArray<RouteCall> Calls { get; } = calls;

    public ImmutableArray<RouteValue> ExternalValues { get; } = externalValues;
}
