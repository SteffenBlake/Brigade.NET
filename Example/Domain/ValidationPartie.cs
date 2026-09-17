using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain;

public sealed class ValidationPartie : IPartie<Unit, Unit>
{
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        Unit ctx,
        TQuery query,
        Next<Unit, TResult> next,
        CancellationToken ct
    ) => ValidateAsync(query!, next);

    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        Unit ctx,
        TCommand command,
        Next<Unit, TResult> next,
        CancellationToken ct
    ) => ValidateAsync(command!, next);

    private static ValueTask<Result<TResult>> ValidateAsync<TResult>(object request, Next<Unit, TResult> next)
    {
        if (request is IValidatable validatable)
        {
            return new ValueTask<Result<TResult>>(
                validatable.Validate().FlatMapAsync(value => next(value).AsTask())
            );
        }

        return next(Unit.Default);
    }
}
