using System.Text;

namespace Brigade.Net.Mise.SqlServer;

/// <summary>SQL Server syntax for Mise builders. Paging requires ORDER BY and a positive FETCH count.</summary>
public sealed class SqlServerDialect : SqlDialect
{
    /// <inheritdoc />
    public override string Name => "SqlServer";

    /// <inheritdoc />
    public override bool UsesRecursiveKeyword => false;

    /// <inheritdoc />
    public override bool RequiresOrderByForPaging => true;

    /// <inheritdoc />
    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }

    /// <inheritdoc />
    public override void AppendPaging(
        StringBuilder text,
        int? limit,
        int? offset
    )
    {
        if (limit == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "SQL Server FETCH requires at least one row.");
        }
        if (limit is null && offset is null)
        {
            return;
        }
        text.Append(" OFFSET ")
            .Append((offset ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append(" ROWS");
        if (limit is int take)
        {
            text.Append(" FETCH NEXT ")
                .Append(take.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(" ROWS ONLY");
        }
    }
}
