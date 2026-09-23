using System;
using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Engines.PostgreSQL;

[Generator]
public sealed class MiseEngineGenerator : IIncrementalGenerator
{
    private static readonly MiseEngineOptions Engine = new(
        "PostgreSQL",
        "Brigade.Net.Mise.PostgreSQL.PostgreSqlTableAttribute",
        "Brigade.Net.Mise.PostgreSQL.PostgreSqlRowAttribute",
        StringComparer.Ordinal,
        identifier => "\"" + identifier.Replace("\"", "\"\"") + "\"",
        "Brigade.Net.Mise.PostgreSQL.MiseSchemaAttribute"
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = MiseGeneratorCore.CreateTargets(context, Engine);
        context.RegisterSourceOutput(targets.Tables, MiseGeneratorCore.EmitTarget);
        context.RegisterSourceOutput(targets.Rows, MiseGeneratorCore.EmitTarget);
    }
}
