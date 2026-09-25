using System;
using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Engines.SqlServer;

[Generator]
public sealed class MiseEngineGenerator : IIncrementalGenerator
{
    private static readonly MiseEngineOptions Engine = new(
        "SqlServer",
        "Brigade.Net.Mise.SqlServer.SqlServerTableAttribute",
        "Brigade.Net.Mise.SqlServer.MiseAttribute",
        StringComparer.OrdinalIgnoreCase,
        identifier => "[" + identifier.Replace("]", "]]") + "]",
        "Brigade.Net.Mise.SqlServer.SchemaAttribute",
        ordinalNamesIgnoreCase: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = MiseGeneratorCore.CreateTargets(context, Engine);

        context.RegisterSourceOutput(targets.Tables, MiseGeneratorCore.EmitTarget);
        context.RegisterSourceOutput(targets.Rows, MiseGeneratorCore.EmitTarget);
    }
}
