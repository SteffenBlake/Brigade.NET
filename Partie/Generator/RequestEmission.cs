using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

public sealed class RequestEmission(
    string typeName,
    string metadata,
    ImmutableArray<RequestPropertyEmission> properties,
    string dtoTypeName,
    string dtoHintName
)
{
    public string TypeName { get; } = typeName;
    public string Metadata { get; } = metadata;
    public ImmutableArray<RequestPropertyEmission> Properties { get; } = properties;
    public string DtoTypeName { get; } = dtoTypeName;
    public string DtoHintName { get; } = dtoHintName;
}
