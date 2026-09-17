using System.Text.Json.Serialization;

namespace Brigade.Net.Core.Results;

/// <summary>
/// Union type over the outcomes a single business-logic request can produce.
/// Each case is a sealed subclass that knows how to describe itself, so
/// consumers never need to switch on a discriminator to use it.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
[JsonConverter(typeof(ResultJsonConverterFactory))]
public abstract class Result<T>
{
    private protected Result()
    {
    }

    /// <summary>
    /// Wraps a success value in a <see cref="Success{T}" /> result.
    /// </summary>
    public static implicit operator Result<T>(T value) => new Success<T>(value);

    /// <summary>
    /// Wraps an <see cref="Results.Error" /> in a <see cref="Failure{T, TFailure}" /> result.
    /// </summary>
    public static implicit operator Result<T>(Error value) => new Failure<T, Error>(value);

    /// <summary>
    /// Wraps a <see cref="Results.NotFound" /> in a <see cref="Failure{T, TFailure}" /> result.
    /// </summary>
    public static implicit operator Result<T>(NotFound value) => new Failure<T, NotFound>(value);

    /// <summary>
    /// Wraps a <see cref="Results.Conflict" /> in a <see cref="Failure{T, TFailure}" /> result.
    /// </summary>
    public static implicit operator Result<T>(Conflict value) => new Failure<T, Conflict>(value);

    /// <summary>
    /// Wraps a <see cref="Results.Forbidden" /> in a <see cref="Failure{T, TFailure}" /> result.
    /// </summary>
    public static implicit operator Result<T>(Forbidden value) => new Failure<T, Forbidden>(value);

    /// <summary>
    /// Wraps a <see cref="Results.GatewayError" /> in a <see cref="Failure{T, TFailure}" /> result.
    /// </summary>
    public static implicit operator Result<T>(GatewayError value) => new Failure<T, GatewayError>(value);

    /// <summary>
    /// Wraps a <see cref="Results.TimeoutResult" /> in a <see cref="Failure{T, TFailure}" /> result.
    /// </summary>
    public static implicit operator Result<T>(TimeoutResult value) => new Failure<T, TimeoutResult>(value);

    /// <summary>
    /// Attempts to read this result as a success.
    /// </summary>
    /// <param name="success">The success value, if this result is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this result is a success.</returns>
    public virtual bool IsSuccess(out T success)
    {
        success = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this result as a deprecated success.
    /// </summary>
    /// <param name="deprecated">The value, if this result is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this result is a deprecated success.</returns>
    public virtual bool IsDeprecated(out Deprecated<T> deprecated)
    {
        deprecated = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this result as an <see cref="Results.Error" /> failure.
    /// </summary>
    /// <param name="error">The error, if this result is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this result is an <see cref="Results.Error" /> failure.</returns>
    public virtual bool IsError(out Error error)
    {
        error = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this result as a <see cref="Results.NotFound" /> failure.
    /// </summary>
    /// <param name="notFound">The value, if this result is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this result is a <see cref="Results.NotFound" /> failure.</returns>
    public virtual bool IsNotFound(out NotFound notFound)
    {
        notFound = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this result as a <see cref="Results.Conflict" /> failure.
    /// </summary>
    /// <param name="conflict">The value, if this result is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this result is a <see cref="Results.Conflict" /> failure.</returns>
    public virtual bool IsConflict(out Conflict conflict)
    {
        conflict = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this result as a <see cref="Results.Forbidden" /> failure.
    /// </summary>
    /// <param name="forbidden">The value, if this result is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this result is a <see cref="Results.Forbidden" /> failure.</returns>
    public virtual bool IsForbidden(out Forbidden forbidden)
    {
        forbidden = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this result as a <see cref="Results.GatewayError" /> failure.
    /// </summary>
    /// <param name="gatewayError">The value, if this result is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this result is a <see cref="Results.GatewayError" /> failure.</returns>
    public virtual bool IsGatewayError(out GatewayError gatewayError)
    {
        gatewayError = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this result as a <see cref="Results.TimeoutResult" /> failure.
    /// </summary>
    /// <param name="timeout">The value, if this result is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this result is a <see cref="Results.TimeoutResult" /> failure.</returns>
    public virtual bool IsTimeout(out TimeoutResult timeout)
    {
        timeout = default!;
        return false;
    }

    /// <summary>
    /// Maps the success value to a new type. Only <see cref="Success{T}" />/<see cref="Deprecated{T}" />
    /// invoke <paramref name="mapper" />; every failure case just carries itself forward as <see cref="Result{TOut}" />.
    /// </summary>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="mapper">Invoked with the success value to produce the mapped value.</param>
    public abstract Result<TOut> Map<TOut>(Func<T, TOut> mapper);

    /// <summary>
    /// The asynchronous equivalent of <see cref="Map{TOut}" />.
    /// </summary>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="mapper">Invoked with the success value to produce the mapped value.</param>
    public abstract Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper);
}
