using System;
using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Engines.MariaDb;

[Generator]
public sealed class MiseEngineGenerator : IIncrementalGenerator
{
    private static readonly MiseEngineOptions Engine = new(
        "MariaDb",
        "Brigade.Net.Mise.MariaDb.MariaDbTableAttribute",
        "Brigade.Net.Mise.MariaDb.MiseAttribute",
        StringComparer.OrdinalIgnoreCase,
        identifier => "`" + identifier.Replace("`", "``") + "`",
        "Brigade.Net.Mise.MariaDb.DatabaseAttribute",
        ordinalNamesIgnoreCase: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = MiseGeneratorCore.CreateTargets(context, Engine);
        context.RegisterSourceOutput(targets.Tables, MiseGeneratorCore.EmitTarget);
        context.RegisterSourceOutput(targets.Rows, MiseGeneratorCore.EmitTarget);
    }
}
