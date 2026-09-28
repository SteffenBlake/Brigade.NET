using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace Brigade.Net.Partie.Extensions.Mise.SqlServer;

/// <summary>Reads a named SqlServer connection string from application configuration.</summary>
public sealed record SqlServerDbConfigProviderContext(
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

        return new DbRouteConfig(connectionString, SqlClientFactory.Instance);
    }
}

/// <summary>Provides a named connection string with the SqlServer ADO.NET factory.</summary>
public sealed class SqlServerDbConfigProvider<TRequest, TResult> :
    IQueryProvider<IDbConfig, SqlServerDbConfigProviderContext, TRequest, TResult>,
    ICommandProvider<IDbConfig, SqlServerDbConfigProviderContext, TRequest, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        SqlServerDbConfigProviderContext ctx,
        TRequest query,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        SqlServerDbConfigProviderContext ctx,
        TRequest command,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }
}
