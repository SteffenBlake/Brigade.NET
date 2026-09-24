using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator;

internal sealed record MiseTargetModel(
    INamedTypeSymbol Type,
    MiseTableModel? Table,
    MiseRowModel? Row,
    ImmutableArray<ColumnModel> Columns,
    ImmutableArray<AliasModel> Aliases,
    ImmutableArray<RelationshipModel> Relationships
);
