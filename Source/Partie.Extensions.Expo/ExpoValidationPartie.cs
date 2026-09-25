using Brigade.Net.Core.Results;
using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Partie.Extensions.Expo;

/// <summary>Stops query and command pipelines when Expo validation fails.</summary>
public sealed class ExpoValidationPartie<TRequest, TResult>
    :
    IQueryPartie<Unit, Unit, TRequest, TResult>,
    ICommandPartie<Unit, Unit, TRequest, TResult>
    where TRequest : IExpoValidatable
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        Unit ctx,
        TRequest query,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ValidateAsync(query, next);
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        Unit ctx,
        TRequest command,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ValidateAsync(command, next);
    }

    private static ValueTask<Result<TResult>> ValidateAsync(
        TRequest request,
        Next<Unit, TResult> next
    )
    {
        if (request.TryValidate(out var errors))
        {
            return next(Unit.Default);
        }

        return ValueTask.FromResult<Result<TResult>>(new Error(errors));
    }
}
