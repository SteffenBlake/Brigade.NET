using Brigade.Net.Core.Results;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Provides a named connection string with the registered ADO.NET factory.</summary>
public sealed class MiseConfigProvider<TRequest, TResult> :
    IQueryProvider<IMiseConfig, MiseConfigProviderContext, TRequest, TResult>,
    ICommandProvider<IMiseConfig, MiseConfigProviderContext, TRequest, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        MiseConfigProviderContext ctx,
        TRequest query,
        Next<IMiseConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        MiseConfigProviderContext ctx,
        TRequest command,
        Next<IMiseConfig, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.CreateConfig());
    }
}
