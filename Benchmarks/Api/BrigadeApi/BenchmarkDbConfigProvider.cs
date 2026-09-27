using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Partie;
using Brigade.Net.Partie.Extensions.Mise;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Npgsql;
using System.Data.Common;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record BenchmarkDbConfigProviderContext(
    [Inject] IConfiguration Configuration,
    [Parameter] string Database
)
{
    public IDbConfig CreateConfig()
    {
        var connectionString = Configuration.GetConnectionString(Database)
            ?? throw new InvalidOperationException($"Missing {Database} connection string.");
        DbProviderFactory factory = Database switch
        {
            "sqlserver" => SqlClientFactory.Instance,
            "postgresql" => NpgsqlFactory.Instance,
            "mysql" or "mariadb" => MySqlConnectorFactory.Instance,
            "sqlite" => SqliteFactory.Instance,
            _ => throw new ArgumentOutOfRangeException(nameof(Database))
        };
        return new DbRouteConfig(connectionString, factory);
    }
}

public sealed class BenchmarkDbConfigProvider<TRequest, TResult>
    : IQueryProvider<IDbConfig, BenchmarkDbConfigProviderContext, TRequest, TResult>,
      ICommandProvider<IDbConfig, BenchmarkDbConfigProviderContext, TRequest, TResult>
{
    public static ValueTask<Result<TResult>> OnQueryAsync(
        BenchmarkDbConfigProviderContext context,
        TRequest query,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(context.CreateConfig());
    }

    public static ValueTask<Result<TResult>> OnCommandAsync(
        BenchmarkDbConfigProviderContext context,
        TRequest command,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(context.CreateConfig());
    }
}
