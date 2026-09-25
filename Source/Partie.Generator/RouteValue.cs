using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteValue(
    ITypeSymbol type,
    int id,
    IParameterSymbol? externalParameter
)
{
    public ITypeSymbol Type { get; } = type;

    public int Id { get; } = id;

    public IParameterSymbol? ExternalParameter { get; } = externalParameter;
}
