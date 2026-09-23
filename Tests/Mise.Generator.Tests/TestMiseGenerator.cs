using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

internal sealed class TestMiseGenerator(string engineName) : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        MiseGeneratorCore.Register(context, engineName);
    }
}
