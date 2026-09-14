using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures;

public sealed class ValidationPartie : IPartie<Unit, EmptyContext>
{
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        EmptyContext ctx,
        TQuery query,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
        where TQuery : class => ValidateAsync(query, next);
    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        EmptyContext ctx,
        TCommand command,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
        where TCommand : class => ValidateAsync(command, next);
    private static ValueTask<Result<TResult>> ValidateAsync<TResult>(object request, Next<Unit, TResult> next)
    {
        if (request is IValidatable validatable)
        {
            return new ValueTask<Result<TResult>>(validatable.Validate().FlatMapAsync(value => next(value).AsTask()));
        }

        return next(Unit.Default);
    }
}
