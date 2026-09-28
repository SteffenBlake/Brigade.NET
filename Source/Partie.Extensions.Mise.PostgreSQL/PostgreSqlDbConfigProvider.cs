using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Brigade.Net.Partie.Extensions.Mise.PostgreSQL;

/// <summary>Reads a named PostgreSql connection string from application configuration.</summary>
public sealed record PostgreSqlDbConfigProviderContext(
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

        return new DbRouteConfig(connectionString, NpgsqlFactory.Instance);
    }
}

/// <summary>Provides a named connection string with the PostgreSql ADO.NET factory.</summary>
public sealed class PostgreSqlDbConfigProvider<TRequest, TResult> :
    IQueryProvider<IDbConfig, PostgreSqlDbConfigProviderContext, TRequest, TResult>,
    ICommandProvider<IDbConfig, PostgreSqlDbConfigProviderContext, TRequest, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        PostgreSqlDbConfigProviderContext ctx,
        TRequest query,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        PostgreSqlDbConfigProviderContext ctx,
        TRequest command,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }
}
