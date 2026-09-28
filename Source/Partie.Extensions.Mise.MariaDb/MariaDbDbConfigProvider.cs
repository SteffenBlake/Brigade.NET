using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Brigade.Net.Partie.Extensions.Mise.MariaDb;

/// <summary>Reads a named MariaDb connection string from application configuration.</summary>
public sealed record MariaDbDbConfigProviderContext(
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

        return new DbRouteConfig(connectionString, MySqlConnectorFactory.Instance);
    }
}

/// <summary>Provides a named connection string with the MariaDb ADO.NET factory.</summary>
public sealed class MariaDbDbConfigProvider<TRequest, TResult> :
    IQueryProvider<IDbConfig, MariaDbDbConfigProviderContext, TRequest, TResult>,
    ICommandProvider<IDbConfig, MariaDbDbConfigProviderContext, TRequest, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        MariaDbDbConfigProviderContext ctx,
        TRequest query,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        MariaDbDbConfigProviderContext ctx,
        TRequest command,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }
}
