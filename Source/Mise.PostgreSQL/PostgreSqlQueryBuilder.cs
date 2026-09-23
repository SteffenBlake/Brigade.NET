namespace Brigade.Net.Mise.PostgreSQL;

/// <summary>A PostgreSQL read builder.</summary>
public sealed class PostgreSqlQueryBuilder() : QueryBuilder(new PostgreSqlDialect());
