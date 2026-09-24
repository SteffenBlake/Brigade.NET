using System.Text;
using System.Globalization;
namespace Brigade.Net.Mise.PostgreSQL;

/// <summary>PostgreSQL syntax for Mise builders.</summary>
public sealed class PostgreSqlDialect : SqlDialect
{
    /// <inheritdoc />
    public override string Name => "PostgreSQL";

    /// <inheritdoc />
    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    /// <inheritdoc />
    public override void AppendPaging(
        StringBuilder text,
        int? limit,
        int? offset
    )
    {
        if (limit is int take)
        {
            text.Append(" LIMIT ").Append(take.ToString(CultureInfo.InvariantCulture));
        }
        if (offset is int skip)
        {
            text.Append(" OFFSET ").Append(skip.ToString(CultureInfo.InvariantCulture));
        }
    }
}
