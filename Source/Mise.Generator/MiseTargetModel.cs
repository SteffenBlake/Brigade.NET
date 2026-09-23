using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator;

internal sealed record MiseTargetModel(
    INamedTypeSymbol Type,
    MiseTableModel? Table,
    MiseRowModel? Row,
    ImmutableArray<MiseColumnModel> Columns,
    ImmutableArray<MiseAliasModel> Aliases,
    ImmutableArray<MiseRelationshipModel> Relationships
);
