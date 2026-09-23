namespace Brigade.Net.Mise.MariaDb;

/// <summary>A MariaDB write builder. INSERT RETURNING requires MariaDB 10.5 or later.</summary>
public sealed class MariaDbCommandBuilder() : CommandBuilder(new MariaDbDialect())
{
    private string? _returning;

    /// <summary>Returns named columns from an INSERT or DELETE through RETURNING.</summary>
    public MariaDbCommandBuilder Returning(params string[] columns)
    {
        if (_returning is not null || columns.Length == 0)
        {
            throw new InvalidOperationException("RETURNING columns must be set once and cannot be empty.");
        }
        var dialect = new MariaDbDialect();
        _returning = "RETURNING " + string.Join(", ", columns.Select(dialect.QuoteIdentifier));
        return this;
    }

    /// <inheritdoc />
    protected override string? TrailingSql()
    {
        if (_returning is not null && Kind is not "INSERT INTO" and not "DELETE FROM")
        {
            throw new InvalidOperationException("MariaDB RETURNING requires INSERT or DELETE.");
        }
        return _returning;
    }
}
