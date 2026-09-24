namespace Microsoft.Extensions.Hosting;

/// <summary>Names used for Aspire services and connection strings.</summary>
public static class ServiceNames
{
    /// <summary>SQL Server service name.</summary>
    public const string SqlServer = nameof(SqlServer);

    /// <summary>PostgreSQL service name.</summary>
    public const string PostgreSql = nameof(PostgreSql);

    /// <summary>SQLite service name.</summary>
    public const string Sqlite = nameof(Sqlite);

    /// <summary>MySQL service name.</summary>
    public const string MySql = nameof(MySql);

    /// <summary>MariaDB service name.</summary>
    public const string MariaDb = nameof(MariaDb);

    /// <summary>Web application service name.</summary>
    public const string WebApp = nameof(WebApp);
}
