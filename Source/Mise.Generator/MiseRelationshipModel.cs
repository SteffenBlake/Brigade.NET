using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator;

internal sealed record MiseRelationshipModel(
    string Name,
    INamedTypeSymbol? Target,
    string SourceColumn,
    string TargetColumn
);
