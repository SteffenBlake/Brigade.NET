using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator;

public sealed class MiseEngineOptions(
    string name,
    string tableAttributeMetadataName,
    string rowAttributeMetadataName,
    StringComparer identifierComparer,
    Func<string, string> quoteIdentifier,
    string? qualifierAttributeMetadataName = null,
    Func<INamedTypeSymbol, CancellationToken, ImmutableArray<Diagnostic>>? validateTarget = null,
    Func<INamedTypeSymbol, CancellationToken, string>? emitExtraMembers = null
)
{
    public string Name { get; } = name;

    public string TableAttributeMetadataName { get; } = tableAttributeMetadataName;

    public string RowAttributeMetadataName { get; } = rowAttributeMetadataName;

    public StringComparer IdentifierComparer { get; } = identifierComparer;

    public Func<string, string> QuoteIdentifier { get; } = quoteIdentifier;

    public string? QualifierAttributeMetadataName { get; } = qualifierAttributeMetadataName;

    public Func<INamedTypeSymbol, CancellationToken, ImmutableArray<Diagnostic>>? ValidateTarget { get; } = validateTarget;

    public Func<INamedTypeSymbol, CancellationToken, string>? EmitExtraMembers { get; } = emitExtraMembers;

    internal static MiseEngineOptions Create(string name)
    {
        if (name == "SqlServer")
        {
            return new MiseEngineOptions(
                name,
                "Brigade.Net.Mise.SqlServer.SqlServerTableAttribute",
                "Brigade.Net.Mise.SqlServer.SqlServerRowAttribute",
                StringComparer.OrdinalIgnoreCase,
                identifier => "[" + identifier.Replace("]", "]]") + "]",
                "Brigade.Net.Mise.SqlServer.MiseSchemaAttribute"
            );
        }

        if (name == "PostgreSQL")
        {
            return new MiseEngineOptions(
                name,
                "Brigade.Net.Mise.PostgreSQL.PostgreSqlTableAttribute",
                "Brigade.Net.Mise.PostgreSQL.PostgreSqlRowAttribute",
                StringComparer.Ordinal,
                QuoteDouble,
                "Brigade.Net.Mise.PostgreSQL.MiseSchemaAttribute"
            );
        }

        if (name is "MySQL" or "MariaDb")
        {
            var runtimeName = name == "MySQL" ? "MySQL" : "MariaDb";
            return new MiseEngineOptions(
                name,
                "Brigade.Net.Mise." + runtimeName + "." + (name == "MySQL" ? "MySql" : "MariaDb") + "TableAttribute",
                "Brigade.Net.Mise." + runtimeName + "." + (name == "MySQL" ? "MySql" : "MariaDb") + "RowAttribute",
                StringComparer.OrdinalIgnoreCase,
                identifier => "`" + identifier.Replace("`", "``") + "`",
                "Brigade.Net.Mise." + runtimeName + ".MiseDatabaseAttribute"
            );
        }

        return new MiseEngineOptions(
            name,
            "Brigade.Net.Mise.SQLite.SqliteTableAttribute",
            "Brigade.Net.Mise.SQLite.SqliteRowAttribute",
            StringComparer.OrdinalIgnoreCase,
            QuoteDouble
        );
    }

    private static string QuoteDouble(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
