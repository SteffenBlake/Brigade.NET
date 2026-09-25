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
    Func<INamedTypeSymbol, CancellationToken, string>? emitExtraMembers = null,
    bool ordinalNamesIgnoreCase = false
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

    public bool OrdinalNamesIgnoreCase { get; } = ordinalNamesIgnoreCase;

    internal static MiseEngineOptions Create(string name)
    {
        if (name == "SqlServer")
        {
            return new MiseEngineOptions(
                name,
                "Brigade.Net.Mise.SqlServer.SqlServerTableAttribute",
                "Brigade.Net.Mise.SqlServer.MiseAttribute",
                StringComparer.OrdinalIgnoreCase,
                identifier => "[" + identifier.Replace("]", "]]") + "]",
                "Brigade.Net.Mise.SqlServer.SchemaAttribute",
                ordinalNamesIgnoreCase: true
            );
        }

        if (name == "PostgreSQL")
        {
            return new MiseEngineOptions(
                name,
                "Brigade.Net.Mise.PostgreSQL.PostgreSqlTableAttribute",
                "Brigade.Net.Mise.PostgreSQL.MiseAttribute",
                StringComparer.Ordinal,
                QuoteDouble,
                "Brigade.Net.Mise.PostgreSQL.SchemaAttribute"
            );
        }

        if (name is "MySQL" or "MariaDb")
        {
            var runtimeName = name == "MySQL" ? "MySQL" : "MariaDb";
            return new MiseEngineOptions(
                name,
                "Brigade.Net.Mise." + runtimeName + "."
                    + (name == "MySQL" ? "MySql" : "MariaDb")
                    + "TableAttribute",
                "Brigade.Net.Mise." + runtimeName + ".MiseAttribute",
                StringComparer.OrdinalIgnoreCase,
                identifier => "`" + identifier.Replace("`", "``") + "`",
                "Brigade.Net.Mise." + runtimeName + ".DatabaseAttribute",
                ordinalNamesIgnoreCase: true
            );
        }

        return new MiseEngineOptions(
            name,
            "Brigade.Net.Mise.SQLite.SqliteTableAttribute",
            "Brigade.Net.Mise.SQLite.MiseAttribute",
            StringComparer.OrdinalIgnoreCase,
            QuoteDouble,
            ordinalNamesIgnoreCase: true
        );
    }

    private static string QuoteDouble(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
