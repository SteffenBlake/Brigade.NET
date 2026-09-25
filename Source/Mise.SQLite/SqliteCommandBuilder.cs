namespace Brigade.Net.Mise.SQLite;

/// <summary>A SQLite write builder.</summary>
public sealed class SqliteCommandBuilder() : CommandBuilder(new SqliteDialect())
{
    private string? _returning;

    /// <summary>Returns named columns from a write through RETURNING.</summary>
    public SqliteCommandBuilder Returning(params string[] columns)
    {
        if (_returning is not null || columns.Length == 0)
        {
            throw new InvalidOperationException("RETURNING columns must be set once and cannot be empty.");
        }

        var dialect = new SqliteDialect();
        _returning = "RETURNING " + string.Join(", ", columns.Select(dialect.QuoteIdentifier));
        return this;
    }

    /// <inheritdoc />
    protected override string? TrailingSql()
    {
        if (_returning is not null && Kind is not "INSERT INTO" and not "UPDATE" and not "DELETE FROM")
        {
            throw new InvalidOperationException("RETURNING requires INSERT, UPDATE, or DELETE.");
        }
        return _returning;
    }
}
