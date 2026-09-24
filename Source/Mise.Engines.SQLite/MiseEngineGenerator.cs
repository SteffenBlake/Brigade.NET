using System;
using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Engines.SQLite;

[Generator]
public sealed class MiseEngineGenerator : IIncrementalGenerator
{
    private static readonly MiseEngineOptions Engine = new(
        "SQLite",
        "Brigade.Net.Mise.SQLite.SqliteTableAttribute",
        "Brigade.Net.Mise.SQLite.MiseAttribute",
        StringComparer.OrdinalIgnoreCase,
        identifier => "\"" + identifier.Replace("\"", "\"\"") + "\"",
        ordinalNamesIgnoreCase: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = MiseGeneratorCore.CreateTargets(context, Engine);
        context.RegisterSourceOutput(targets.Tables, MiseGeneratorCore.EmitTarget);
        context.RegisterSourceOutput(targets.Rows, MiseGeneratorCore.EmitTarget);
    }
}
