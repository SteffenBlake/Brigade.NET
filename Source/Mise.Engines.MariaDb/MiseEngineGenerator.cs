using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Engines.MariaDb;

[Generator]
public sealed class MiseEngineGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        MiseGeneratorCore.Register(context, "MariaDb");
    }
}
