using System.Collections.Generic;
using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteDeclaration(IEnumerable<string> path, string operation)
{
    public ImmutableArray<string> Path { get; } = path.ToImmutableArray();
    public string Operation { get; } = operation;
}
