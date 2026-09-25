using System.Globalization;
using System.Text;

namespace Brigade.Net.Mise.MariaDb;

/// <summary>MariaDB syntax for Mise builders.</summary>
public sealed class MariaDbDialect : SqlDialect
{
    /// <inheritdoc />
    public override string Name => "MariaDB";

    /// <inheritdoc />
    public override bool SupportsFullJoin => false;

    /// <inheritdoc />
    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return "`" + identifier.Replace("`", "``", StringComparison.Ordinal) + "`";
    }

    /// <inheritdoc />
    public override void AppendPaging(
        StringBuilder text,
        int? limit,
        int? offset
    )
    {
        if (limit is null && offset is null)
        {
            return;
        }
        text.Append(" LIMIT ")
            .Append(limit?.ToString(CultureInfo.InvariantCulture) ?? "18446744073709551615");
        if (offset is int skip)
        {
            text.Append(" OFFSET ").Append(skip.ToString(CultureInfo.InvariantCulture));
        }
    }
}
