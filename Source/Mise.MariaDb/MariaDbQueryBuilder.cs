namespace Brigade.Net.Mise.MariaDb;

/// <summary>A MariaDB read builder.</summary>
public sealed class MariaDbQueryBuilder() : QueryBuilder(new MariaDbDialect());
