using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteValue(ITypeSymbol type, int id, IParameterSymbol? externalParameter)
{
    public ITypeSymbol Type { get; } = type;
    public int Id { get; } = id;
    public IParameterSymbol? ExternalParameter { get; } = externalParameter;
}

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

public sealed class RouteGraphResult(RouteGraph? graph, ImmutableArray<Diagnostic> diagnostics)
{
    public RouteGraph? Graph { get; } = graph;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}