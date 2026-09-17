using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator.Tests;

internal sealed class BrigadeRoutingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) => BrigadeGeneratorCore.Initialize(context);
}