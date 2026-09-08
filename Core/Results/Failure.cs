namespace Brigade.Net.Core.Results;

/// <summary>
/// Single generic wrapper so every non-generic failure payload can become a
/// <see cref="Result{T}" /> without needing a bespoke type per failure case.
/// </summary>
/// <typeparam name="T">The success value type this result would have carried.</typeparam>
/// <typeparam name="TFailure">The type of the wrapped failure payload.</typeparam>
/// <param name="value">The wrapped failure payload.</param>
[System.Text.Json.Serialization.JsonConverter(typeof(ResultJsonConverterFactory))]
public sealed class Failure<T, TFailure>(TFailure value) : Result<T>
    where TFailure : FailureBase
{
    /// <summary>
    /// The wrapped failure payload.
    /// </summary>
    public TFailure Value { get; } = value;

    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? string.Empty;

    /// <inheritdoc />
    public override bool IsError(out Error error) => Value.IsError(out error);

    /// <inheritdoc />
    public override bool IsNotFound(out NotFound notFound) => Value.IsNotFound(out notFound);

    /// <inheritdoc />
    public override bool IsConflict(out Conflict conflict) => Value.IsConflict(out conflict);

    /// <inheritdoc />
    public override bool IsForbidden(out Forbidden forbidden) => Value.IsForbidden(out forbidden);

    /// <inheritdoc />
    public override bool IsGatewayError(out GatewayError gatewayError) => Value.IsGatewayError(out gatewayError);

    /// <inheritdoc />
    public override bool IsTimeout(out TimeoutResult timeout) => Value.IsTimeout(out timeout);

    /// <inheritdoc />
    public override Result<T2> Map<T2>(Func<T, T2> mapper) => new Failure<T2, TFailure>(Value);

    /// <inheritdoc />
    public override Task<Result<T2>> MapAsync<T2>(Func<T, Task<T2>> mapper) =>
        Task.FromResult<Result<T2>>(new Failure<T2, TFailure>(Value));
}
