namespace Brigade.Net.Mise.MySQL;

/// <summary>A MySQL read builder.</summary>
public sealed class MySqlQueryBuilder() : QueryBuilder(new MySqlDialect())
{
    /// <summary>Reads the last generated auto-increment ID on the same connection.</summary>
    public static IQueryBuilder LastInsertId() => new MySqlQueryBuilder().Select($"LAST_INSERT_ID()");
}
