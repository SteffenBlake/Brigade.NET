using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace Brigade.Net.Partie.Extensions.Mise.SQLite;

/// <summary>Reads a named Sqlite connection string from application configuration.</summary>
public sealed record SqliteDbConfigProviderContext(
    [Inject] IConfiguration Configuration,
    [Parameter] string ConnectionStringName
)
{
    /// <summary>Creates the configuration selected by this route.</summary>
    public IDbConfig CreateConfig()
    {
        var connectionString = Configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' was not found."
            );

        return new DbRouteConfig(connectionString, SqliteFactory.Instance);
    }
}

/// <summary>Provides a named connection string with the Sqlite ADO.NET factory.</summary>
public sealed class SqliteDbConfigProvider<TRequest, TResult> :
    IQueryProvider<IDbConfig, SqliteDbConfigProviderContext, TRequest, TResult>,
    ICommandProvider<IDbConfig, SqliteDbConfigProviderContext, TRequest, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        SqliteDbConfigProviderContext ctx,
        TRequest query,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        SqliteDbConfigProviderContext ctx,
        TRequest command,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }
}
