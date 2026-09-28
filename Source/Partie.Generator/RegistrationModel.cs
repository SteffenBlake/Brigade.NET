using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal sealed record RegistrationModel(
    INamedTypeSymbol type,
    AttributeData attribute,
    bool isProvider,
    ImmutableDictionary<string, string>? parameters = null
)
{
    public INamedTypeSymbol Type { get; } = type;

    public bool IsProvider { get; } = isProvider;

    public ImmutableDictionary<string, string> Parameters { get; } = parameters ?? ContextParameters.Arguments(attribute);
}
