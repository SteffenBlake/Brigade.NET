using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

public sealed class RequestEmission(
    string typeName,
    string metadata,
    ImmutableArray<RequestPropertyEmission> properties
)
{
    public string TypeName { get; } = typeName;
    public string Metadata { get; } = metadata;
    public ImmutableArray<RequestPropertyEmission> Properties { get; } = properties;
}
