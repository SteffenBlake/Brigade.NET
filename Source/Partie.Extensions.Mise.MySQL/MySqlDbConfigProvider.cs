using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Brigade.Net.Partie.Extensions.Mise.MySQL;

/// <summary>Reads a named MySql connection string from application configuration.</summary>
public sealed record MySqlDbConfigProviderContext(
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

/// <summary>Provides a named connection string with the MySql ADO.NET factory.</summary>
public sealed class MySqlDbConfigProvider<TRequest, TResult> :
    IQueryProvider<IDbConfig, MySqlDbConfigProviderContext, TRequest, TResult>,
    ICommandProvider<IDbConfig, MySqlDbConfigProviderContext, TRequest, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        MySqlDbConfigProviderContext ctx,
        TRequest query,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        MySqlDbConfigProviderContext ctx,
        TRequest command,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }
}
