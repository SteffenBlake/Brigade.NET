using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures;

public sealed class ValidationPartie<TRequest, TResult> :
    IQueryPartie<Unit, Unit, TRequest, TResult>,
    ICommandPartie<Unit, Unit, TRequest, TResult>
    where TRequest : IValidatable
{
    public static ValueTask<Result<TResult>> OnQueryAsync(
        Unit ctx,
        TRequest query,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ValidateAsync(query, next);
    }

    public static ValueTask<Result<TResult>> OnCommandAsync(
        Unit ctx,
        TRequest command,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ValidateAsync(command, next);
    }

    private static ValueTask<Result<TResult>> ValidateAsync(TRequest request, Next<Unit, TResult> next)
    {
        return new ValueTask<Result<TResult>>(
            request.Validate().FlatMapAsync(value => next(value).AsTask())
        );
    }
}
