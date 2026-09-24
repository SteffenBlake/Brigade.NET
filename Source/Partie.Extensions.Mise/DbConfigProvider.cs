using System.Data.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Reads a named connection string from application configuration.</summary>
public sealed record DbConfigProviderContext(
    [Inject] IConfiguration Configuration,
    [Inject] IServiceProvider Services,
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
        var providerFactory = Services.GetRequiredKeyedService<DbProviderFactory>(ConnectionStringName);
        return new DbRouteConfig(connectionString, providerFactory);
    }
}

/// <summary>Provides a named connection string with the registered ADO.NET factory.</summary>
public sealed class DbConfigProvider<TRequest, TResult> :
    IQueryProvider<IDbConfig, DbConfigProviderContext, TRequest, TResult>,
    ICommandProvider<IDbConfig, DbConfigProviderContext, TRequest, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        DbConfigProviderContext ctx,
        TRequest query,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        DbConfigProviderContext ctx,
        TRequest command,
        Next<IDbConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }
}
