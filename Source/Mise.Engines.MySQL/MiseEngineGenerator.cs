using System;
using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Engines.MySQL;

[Generator]
public sealed class MiseEngineGenerator : IIncrementalGenerator
{
    private static readonly MiseEngineOptions Engine = new(
        "MySQL",
        "Brigade.Net.Mise.MySQL.MySqlTableAttribute",
        "Brigade.Net.Mise.MySQL.MiseAttribute",
        StringComparer.OrdinalIgnoreCase,
        identifier => "`" + identifier.Replace("`", "``") + "`",
        "Brigade.Net.Mise.MySQL.DatabaseAttribute",
        ordinalNamesIgnoreCase: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = MiseGeneratorCore.CreateTargets(context, Engine);
        context.RegisterSourceOutput(targets.Tables, MiseGeneratorCore.EmitTarget);
        context.RegisterSourceOutput(targets.Rows, MiseGeneratorCore.EmitTarget);
    }
}
