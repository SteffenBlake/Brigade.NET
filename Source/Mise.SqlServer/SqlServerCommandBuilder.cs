namespace Brigade.Net.Mise.SqlServer;

/// <summary>A SQL Server write builder.</summary>
public sealed class SqlServerCommandBuilder() : CommandBuilder(new SqlServerDialect())
{
    private string? _output;

    /// <summary>Returns inserted column values through SQL Server OUTPUT.</summary>
    public SqlServerCommandBuilder OutputInserted(params string[] columns)
    {
        if (_output is not null || columns.Length == 0)
        {
            throw new InvalidOperationException("OUTPUT columns must be set once and cannot be empty.");
        }

        var dialect = new SqlServerDialect();
        _output = "OUTPUT " + string.Join(", ",
            columns.Select(column => "INSERTED." + dialect.QuoteIdentifier(column)));
        return this;
    }

    /// <inheritdoc />
    protected override string? InsertAfterTargetSql() => _output;

    /// <inheritdoc />
    protected override string? UpdateAfterSetSql() => _output;

    /// <inheritdoc />
    protected override string? TrailingSql()
    {
        if (_output is not null && Kind is not "INSERT INTO" and not "UPDATE")
        {
            throw new InvalidOperationException("OUTPUT INSERTED requires INSERT or UPDATE.");
        }
        return null;
    }
}
