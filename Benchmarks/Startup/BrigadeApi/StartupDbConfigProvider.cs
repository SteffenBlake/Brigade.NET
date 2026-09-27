using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Partie;
using Brigade.Net.Partie.Extensions.Mise;
using Microsoft.Data.Sqlite;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi;

public sealed record StartupDbConfigProviderContext(
    [Inject] IConfiguration Configuration,
    [Parameter] string Name
)
{
    public IDbConfig CreateConfig()
    {
        var connectionString = Configuration.GetConnectionString(Name)
            ?? throw new InvalidOperationException($"Missing connection string: {Name}");
        return new DbRouteConfig(connectionString, SqliteFactory.Instance);
    }
}

public sealed class StartupDbConfigProvider<TRequest, TResult>
    : IQueryProvider<IDbConfig, StartupDbConfigProviderContext, TRequest, TResult>,
      ICommandProvider<IDbConfig, StartupDbConfigProviderContext, TRequest, TResult>
{
    public static ValueTask<Result<TResult>> OnQueryAsync(
        StartupDbConfigProviderContext context,
        TRequest request,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(context.CreateConfig());
    }

    public static ValueTask<Result<TResult>> OnCommandAsync(
        StartupDbConfigProviderContext context,
        TRequest request,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(context.CreateConfig());
    }
}
