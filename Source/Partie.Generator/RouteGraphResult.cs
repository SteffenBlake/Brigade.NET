using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteGraphResult(
    RouteGraph? graph,
    ImmutableArray<Diagnostic> diagnostics
)
{
    public RouteGraph? Graph { get; } = graph;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}
