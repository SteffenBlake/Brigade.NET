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
        where THandler : IQueryHandler<TQuery, TResult, TContext> where TQuery : class where TContext : class => THandler.RunAsync(ctx, query, ct);
    /// <summary>Invokes a command contract.</summary>
    public static Task<Result<TResult>> Command<THandler, TCommand, TResult, TContext>(
        UnitOfWork uow,
        TContext ctx,
        TCommand cmd,
        CancellationToken ct
    )
        where THandler : ICommandHandler<TCommand, TResult, TContext> where TCommand : class where TContext : class => THandler.RunAsync(uow, ctx, cmd, ct);
    /// <summary>Invokes the query hook of a step contract.</summary>
    public static ValueTask<Result<TResult>> QueryPartie<TPartie, TProvided, TContext, TQuery, TResult>(
        TContext ctx,
        TQuery query,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
        where TPartie : IPartie<TProvided, TContext> where TContext : class where TQuery : class => TPartie.OnQueryAsync<TQuery, TResult>(ctx, query, next, ct);
    /// <summary>Invokes the command hook of a step contract.</summary>
    public static ValueTask<Result<TResult>> CommandPartie<TPartie, TProvided, TContext, TCommand, TResult>(
        TContext ctx,
        TCommand command,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
        where TPartie : IPartie<TProvided, TContext> where TContext : class where TCommand : class => TPartie.OnCommandAsync<TCommand, TResult>(ctx, command, next, ct);
}
