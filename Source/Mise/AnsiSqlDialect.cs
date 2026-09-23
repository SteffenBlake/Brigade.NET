namespace Brigade.Net.Mise;

internal sealed class AnsiSqlDialect : SqlDialect
{
    public override string Name => "ANSI";

    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
