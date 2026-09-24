using System.Text;
using System.Globalization;
namespace Brigade.Net.Mise.SQLite;

/// <summary>SQLite syntax for Mise builders. RIGHT and FULL JOIN require SQLite 3.39 or later.</summary>
public sealed class SqliteDialect : SqlDialect
{
    /// <inheritdoc />
    public override string Name => "SQLite";

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
            if (limit is null)
            {
                text.Append(" LIMIT -1");
            }
            text.Append(" OFFSET ").Append(skip.ToString(CultureInfo.InvariantCulture));
        }
    }
}
