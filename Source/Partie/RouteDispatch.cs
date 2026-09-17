using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie;

/// <summary>Dispatches static interface contracts, including explicit implementations.</summary>
public static class RouteDispatch
{
    /// <summary>Invokes a query contract.</summary>
    public static Task<Result<TResult>> Query<THandler, TQuery, TResult, TContext>(
        TContext ctx,
        TQuery query,
        CancellationToken ct
    )
        where THandler : IQueryHandler<TQuery, TResult, TContext>
    {
        return THandler.RunAsync(ctx, query, ct);
    }

    /// <summary>Invokes a command contract.</summary>
    public static Task<Result<TResult>> Command<THandler, TCommand, TResult, TContext>(
        UnitOfWork uow,
        TContext ctx,
        TCommand cmd,
        CancellationToken ct
    )
        where THandler : ICommandHandler<TCommand, TResult, TContext>
    {
        return THandler.RunAsync(uow, ctx, cmd, ct);
    }

    /// <summary>Invokes the query hook of a step contract.</summary>
    public static ValueTask<Result<TResult>> QueryPartie<TPartie, TProvided, TContext, TQuery, TResult>(
        TContext ctx,
        TQuery query,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
        where TPartie : IQueryPartie<TProvided, TContext, TQuery, TResult>
    {
        return TPartie.OnQueryAsync(ctx, query, next, ct);
    }

    /// <summary>Invokes the command hook of a step contract.</summary>
    public static ValueTask<Result<TResult>> CommandPartie<TPartie, TProvided, TContext, TCommand, TResult>(
        TContext ctx,
        TCommand command,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
        where TPartie : ICommandPartie<TProvided, TContext, TCommand, TResult>
    {
        return TPartie.OnCommandAsync(ctx, command, next, ct);
    }

    /// <summary>Invokes the query hook of a provider contract.</summary>
    public static ValueTask<Result<TResult>> QueryProvider<TProvider, TProvided, TContext, TQuery, TResult>(
        TContext ctx,
        TQuery query,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
        where TProvider : IQueryProvider<TProvided, TContext, TQuery, TResult>
    {
        return TProvider.OnQueryAsync(ctx, query, next, ct);
    }

    /// <summary>Invokes the command hook of a provider contract.</summary>
    public static ValueTask<Result<TResult>> CommandProvider<TProvider, TProvided, TContext, TCommand, TResult>(
        TContext ctx,
        TCommand command,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
        where TProvider : ICommandProvider<TProvided, TContext, TCommand, TResult>
    {
        return TProvider.OnCommandAsync(ctx, command, next, ct);
    }
}
