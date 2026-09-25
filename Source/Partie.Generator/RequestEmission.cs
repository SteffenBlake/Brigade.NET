using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

public sealed record RequestEmission(
    string TypeName,
    string Metadata,
    ImmutableArray<RequestPropertyEmission> Properties,
    string DtoTypeName,
    string DtoHintName
);
