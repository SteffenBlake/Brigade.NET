using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

public sealed record RouteDeclaration(
    IEnumerable<string> path,
    string operation,
    INamedTypeSymbol? handler = null,
    ImmutableDictionary<string, string>? parameters = null
)
{
    public ImmutableArray<string> Path { get; } = path.ToImmutableArray();

    public string Operation { get; } = operation;

    public INamedTypeSymbol? Handler { get; } = handler;

    public ImmutableDictionary<string, string> Parameters { get; } = parameters
        ?? ImmutableDictionary<string, string>.Empty;
}
