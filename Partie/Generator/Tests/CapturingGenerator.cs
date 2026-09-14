using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator.Tests;

internal sealed class CapturingGenerator(Action<RouteEmission> capture) : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) => BrigadeGeneratorCore.Initialize(
        context,
        route =>
    {
        capture(route);
        return "";
    }
    );
}
