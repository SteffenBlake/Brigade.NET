using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal static class StepContracts
{
    public static bool IsStep(INamedTypeSymbol type, Compilation compilation)
    {
        return IsProvider(type, compilation)
            || Is(type, "IQueryPartie`4", compilation)
            || Is(type, "ICommandPartie`4", compilation);
    }

    public static bool IsProvider(INamedTypeSymbol type, Compilation compilation)
    {
        return Is(type, "IQueryProvider`4", compilation)
            || Is(type, "ICommandProvider`4", compilation);
    }

    public static bool IsCommand(INamedTypeSymbol type, Compilation compilation)
    {
        return Is(type, "ICommandProvider`4", compilation)
            || Is(type, "ICommandPartie`4", compilation);
    }

    private static bool Is(INamedTypeSymbol type, string name, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(
            type.OriginalDefinition,
            compilation.GetTypeByMetadataName("Brigade.Net.Partie." + name)
        );
    }
}
