namespace Brigade.Net.Mise.SQLite;

/// <summary>A SQLite read builder.</summary>
public sealed class SqliteQueryBuilder() : QueryBuilder(new SqliteDialect())
{
}
