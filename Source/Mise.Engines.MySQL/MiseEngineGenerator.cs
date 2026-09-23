using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Engines.MySQL;

[Generator]
public sealed class MiseEngineGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        MiseGeneratorCore.Register(context, "MySQL");
    }
}
