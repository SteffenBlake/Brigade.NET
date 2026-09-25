using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteCall(
    IMethodSymbol method,
    ImmutableArray<RouteValue> arguments,
    RouteValue? providedValue,
    int continuationParameterIndex,
    bool isProvider
)
{
    public IMethodSymbol Method { get; } = method;

    public ImmutableArray<RouteValue> Arguments { get; } = arguments;

    public RouteValue? ProvidedValue { get; } = providedValue;

    public int ContinuationParameterIndex { get; } = continuationParameterIndex;

    public bool IsProvider { get; } = isProvider;
}
