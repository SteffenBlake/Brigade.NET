using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal sealed class RegistrationModel(INamedTypeSymbol type, AttributeData attribute)
{
    public INamedTypeSymbol Type { get; } = type;
    public ImmutableDictionary<string, string> Parameters { get; } = ContextParameters.Arguments(attribute);
}
