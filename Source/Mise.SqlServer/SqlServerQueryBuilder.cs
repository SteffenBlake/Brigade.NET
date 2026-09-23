namespace Brigade.Net.Mise.SqlServer;

/// <summary>A SQL Server read builder with engine-specific query hints.</summary>
public sealed class SqlServerQueryBuilder() : QueryBuilder(new SqlServerDialect())
{
    private int? _maxRecursion;

    /// <summary>Sets MAXRECURSION once; zero means no limit.</summary>
    public SqlServerQueryBuilder MaxRecursion(int count)
    {
        if (count < 0 || count > 32767 || _maxRecursion is not null)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        _maxRecursion = count;
        return this;
    }

    /// <inheritdoc />
    protected override string? TrailingSql() => _maxRecursion is int count
        ? "OPTION (MAXRECURSION " + count.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")"
        : null;
}
