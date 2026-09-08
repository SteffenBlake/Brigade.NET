namespace Brigade.Net.Core.Results;

/// <summary>
/// Extension methods for composing <see cref="Result{T}" /> values.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps the success value, with the option to recover any failure case into a success value instead
    /// of it short-circuiting. Each failure delegate defaults to <see langword="null" />, which keeps the
    /// same passthrough behavior as the plain <see cref="Result{T}.Map{TOut}" /> overload; a non-null
    /// delegate recovers that case into a success value instead of short-circuiting.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Invoked with the success value to produce the mapped value.</param>
    /// <param name="error">If provided, recovers an <see cref="Error" /> failure into a success value.</param>
    /// <param name="notFound">If provided, recovers a <see cref="NotFound" /> failure into a success value.</param>
    /// <param name="conflict">If provided, recovers a <see cref="Conflict" /> failure into a success value.</param>
    /// <param name="forbidden">If provided, recovers a <see cref="Forbidden" /> failure into a success value.</param>
    /// <param name="gatewayError">If provided, recovers a <see cref="GatewayError" /> failure into a success value.</param>
    /// <param name="timeout">If provided, recovers a <see cref="TimeoutResult" /> failure into a success value.</param>
    public static Result<TOut> Map<T, TOut>(
        this Result<T> result,
        Func<T, TOut> success,
        Func<Error, TOut>? error = null,
        Func<NotFound, TOut>? notFound = null,
        Func<Conflict, TOut>? conflict = null,
        Func<Forbidden, TOut>? forbidden = null,
        Func<GatewayError, TOut>? gatewayError = null,
        Func<TimeoutResult, TOut>? timeout = null)
    {
        if (result.IsError(out var e))
        {
            return error is not null ? new Success<TOut>(error(e)) : new Failure<TOut, Error>(e);
        }

        if (result.IsNotFound(out var nf))
        {
            return notFound is not null ? new Success<TOut>(notFound(nf)) : new Failure<TOut, NotFound>(nf);
        }

        if (result.IsConflict(out var c))
        {
            return conflict is not null ? new Success<TOut>(conflict(c)) : new Failure<TOut, Conflict>(c);
        }

        if (result.IsGatewayError(out var ge))
        {
            return gatewayError is not null ? new Success<TOut>(gatewayError(ge)) : new Failure<TOut, GatewayError>(ge);
        }

        if (result.IsTimeout(out var t))
        {
            return timeout is not null ? new Success<TOut>(timeout(t)) : new Failure<TOut, TimeoutResult>(t);
        }

        if (result.IsForbidden(out var f))
        {
            return forbidden is not null ? new Success<TOut>(forbidden(f)) : new Failure<TOut, Forbidden>(f);
        }

        return result.Map(success);
    }

    /// <summary>
    /// The asynchronous equivalent of the granular <see cref="Map{T, TOut}(Result{T}, Func{T, TOut}, Func{Error, TOut}?, Func{NotFound, TOut}?, Func{Conflict, TOut}?, Func{Forbidden, TOut}?, Func{GatewayError, TOut}?, Func{TimeoutResult, TOut}?)" /> overload.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Invoked with the success value to produce the mapped value.</param>
    /// <param name="error">If provided, recovers an <see cref="Error" /> failure into a success value.</param>
    /// <param name="notFound">If provided, recovers a <see cref="NotFound" /> failure into a success value.</param>
    /// <param name="conflict">If provided, recovers a <see cref="Conflict" /> failure into a success value.</param>
    /// <param name="forbidden">If provided, recovers a <see cref="Forbidden" /> failure into a success value.</param>
    /// <param name="gatewayError">If provided, recovers a <see cref="GatewayError" /> failure into a success value.</param>
    /// <param name="timeout">If provided, recovers a <see cref="TimeoutResult" /> failure into a success value.</param>
    public static async Task<Result<TOut>> MapAsync<T, TOut>(
        this Result<T> result,
        Func<T, Task<TOut>> success,
        Func<Error, Task<TOut>>? error = null,
        Func<NotFound, Task<TOut>>? notFound = null,
        Func<Conflict, Task<TOut>>? conflict = null,
        Func<Forbidden, Task<TOut>>? forbidden = null,
        Func<GatewayError, Task<TOut>>? gatewayError = null,
        Func<TimeoutResult, Task<TOut>>? timeout = null)
    {
        if (result.IsError(out var e))
        {
            return error is not null ? new Success<TOut>(await error(e)) : new Failure<TOut, Error>(e);
        }

        if (result.IsNotFound(out var nf))
        {
            return notFound is not null ? new Success<TOut>(await notFound(nf)) : new Failure<TOut, NotFound>(nf);
        }

        if (result.IsConflict(out var c))
        {
            return conflict is not null ? new Success<TOut>(await conflict(c)) : new Failure<TOut, Conflict>(c);
        }

        if (result.IsGatewayError(out var ge))
        {
            return gatewayError is not null ? new Success<TOut>(await gatewayError(ge)) : new Failure<TOut, GatewayError>(ge);
        }

        if (result.IsTimeout(out var t))
        {
            return timeout is not null ? new Success<TOut>(await timeout(t)) : new Failure<TOut, TimeoutResult>(t);
        }

        if (result.IsForbidden(out var f))
        {
            return forbidden is not null ? new Success<TOut>(await forbidden(f)) : new Failure<TOut, Forbidden>(f);
        }

        return await result.MapAsync(success);
    }

    /// <summary>
    /// Convenience for when every failure case should recover the same way.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Invoked with the success value to produce the mapped value.</param>
    /// <param name="failure">Invoked with any failure case to recover it into a success value.</param>
    public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> success, Func<FailureBase, TOut> failure)
    {
        return result.Map(
            success,
            error: failure,
            notFound: failure,
            conflict: failure,
            forbidden: failure,
            gatewayError: failure,
            timeout: failure);
    }

    /// <summary>
    /// The asynchronous equivalent of <see cref="Map{T, TOut}(Result{T}, Func{T, TOut}, Func{FailureBase, TOut})" />.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Invoked with the success value to produce the mapped value.</param>
    /// <param name="failure">Invoked with any failure case to recover it into a success value.</param>
    public static Task<Result<TOut>> MapAsync<T, TOut>(this Result<T> result, Func<T, Task<TOut>> success, Func<FailureBase, Task<TOut>> failure)
    {
        return result.MapAsync(
            success,
            error: failure,
            notFound: failure,
            conflict: failure,
            forbidden: failure,
            gatewayError: failure,
            timeout: failure);
    }

    /// <summary>
    /// Maps the success value to a new <see cref="Result{TOut}" /> and flattens the result, equivalent to
    /// <c>result.Map(mapper).Flatten()</c>.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="mapper">Invoked with the success value to produce the mapped result.</param>
    public static Result<TOut> FlatMap<T, TOut>(this Result<T> result, Func<T, Result<TOut>> mapper)
    {
        return result.Map(mapper).Flatten();
    }

    /// <summary>
    /// The asynchronous equivalent of <see cref="FlatMap{T, TOut}(Result{T}, Func{T, Result{TOut}})" />.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="mapper">Invoked with the success value to produce the mapped result.</param>
    public static async Task<Result<TOut>> FlatMapAsync<T, TOut>(this Result<T> result, Func<T, Task<Result<TOut>>> mapper)
    {
        var mapped = await result.MapAsync(mapper);

        return mapped.Flatten();
    }

    /// <summary>
    /// The granular equivalent of <see cref="FlatMap{T, TOut}(Result{T}, Func{T, Result{TOut}})" />, equivalent to
    /// <c>result.Map(success, error, notFound, conflict, forbidden, gatewayError, timeout).Flatten()</c>.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Invoked with the success value to produce the mapped result.</param>
    /// <param name="error">If provided, recovers an <see cref="Error" /> failure into a mapped result.</param>
    /// <param name="notFound">If provided, recovers a <see cref="NotFound" /> failure into a mapped result.</param>
    /// <param name="conflict">If provided, recovers a <see cref="Conflict" /> failure into a mapped result.</param>
    /// <param name="forbidden">If provided, recovers a <see cref="Forbidden" /> failure into a mapped result.</param>
    /// <param name="gatewayError">If provided, recovers a <see cref="GatewayError" /> failure into a mapped result.</param>
    /// <param name="timeout">If provided, recovers a <see cref="TimeoutResult" /> failure into a mapped result.</param>
    public static Result<TOut> FlatMap<T, TOut>(
        this Result<T> result,
        Func<T, Result<TOut>> success,
        Func<Error, Result<TOut>>? error = null,
        Func<NotFound, Result<TOut>>? notFound = null,
        Func<Conflict, Result<TOut>>? conflict = null,
        Func<Forbidden, Result<TOut>>? forbidden = null,
        Func<GatewayError, Result<TOut>>? gatewayError = null,
        Func<TimeoutResult, Result<TOut>>? timeout = null)
    {
        return result.Map(success, error, notFound, conflict, forbidden, gatewayError, timeout).Flatten();
    }

    /// <summary>
    /// The asynchronous equivalent of the granular <see cref="FlatMap{T, TOut}(Result{T}, Func{T, Result{TOut}}, Func{Error, Result{TOut}}?, Func{NotFound, Result{TOut}}?, Func{Conflict, Result{TOut}}?, Func{Forbidden, Result{TOut}}?, Func{GatewayError, Result{TOut}}?, Func{TimeoutResult, Result{TOut}}?)" /> overload.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Invoked with the success value to produce the mapped result.</param>
    /// <param name="error">If provided, recovers an <see cref="Error" /> failure into a mapped result.</param>
    /// <param name="notFound">If provided, recovers a <see cref="NotFound" /> failure into a mapped result.</param>
    /// <param name="conflict">If provided, recovers a <see cref="Conflict" /> failure into a mapped result.</param>
    /// <param name="forbidden">If provided, recovers a <see cref="Forbidden" /> failure into a mapped result.</param>
    /// <param name="gatewayError">If provided, recovers a <see cref="GatewayError" /> failure into a mapped result.</param>
    /// <param name="timeout">If provided, recovers a <see cref="TimeoutResult" /> failure into a mapped result.</param>
    public static async Task<Result<TOut>> FlatMapAsync<T, TOut>(
        this Result<T> result,
        Func<T, Task<Result<TOut>>> success,
        Func<Error, Task<Result<TOut>>>? error = null,
        Func<NotFound, Task<Result<TOut>>>? notFound = null,
        Func<Conflict, Task<Result<TOut>>>? conflict = null,
        Func<Forbidden, Task<Result<TOut>>>? forbidden = null,
        Func<GatewayError, Task<Result<TOut>>>? gatewayError = null,
        Func<TimeoutResult, Task<Result<TOut>>>? timeout = null)
    {
        var mapped = await result.MapAsync(success, error, notFound, conflict, forbidden, gatewayError, timeout);

        return mapped.Flatten();
    }

    /// <summary>
    /// The FlatMap equivalent of <see cref="Map{T, TOut}(Result{T}, Func{T, TOut}, Func{FailureBase, TOut})" />.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Invoked with the success value to produce the mapped result.</param>
    /// <param name="failure">Invoked with any failure case to recover it into a mapped result.</param>
    public static Result<TOut> FlatMap<T, TOut>(this Result<T> result, Func<T, Result<TOut>> success, Func<FailureBase, Result<TOut>> failure)
    {
        return result.Map(success, failure).Flatten();
    }

    /// <summary>
    /// The asynchronous equivalent of <see cref="FlatMap{T, TOut}(Result{T}, Func{T, Result{TOut}}, Func{FailureBase, Result{TOut}})" />.
    /// </summary>
    /// <typeparam name="T">The type of the input success value.</typeparam>
    /// <typeparam name="TOut">The type of the mapped success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="success">Invoked with the success value to produce the mapped result.</param>
    /// <param name="failure">Invoked with any failure case to recover it into a mapped result.</param>
    public static async Task<Result<TOut>> FlatMapAsync<T, TOut>(this Result<T> result, Func<T, Task<Result<TOut>>> success, Func<FailureBase, Task<Result<TOut>>> failure)
    {
        var mapped = await result.MapAsync(success, failure);

        return mapped.Flatten();
    }

    /// <summary>
    /// Collapses a <see cref="Result{T}" /> of <see cref="Result{T}" /> into a single <see cref="Result{T}" />.
    /// Checks run roughly common -&gt; esoteric: success first, then the failure
    /// cases, then deprecated last since it needs to recurse into the inner result.
    /// </summary>
    /// <typeparam name="T">The type of the innermost success value.</typeparam>
    /// <param name="result">The nested result to flatten.</param>
    private static Result<T> Flatten<T>(this Result<Result<T>> result)
    {
        if (result.IsSuccess(out var inner))
        {
            return inner;
        }

        if (result.IsError(out var error))
        {
            return error;
        }

        if (result.IsNotFound(out var notFound))
        {
            return notFound;
        }

        if (result.IsConflict(out var conflict))
        {
            return conflict;
        }

        if (result.IsGatewayError(out var gatewayError))
        {
            return gatewayError;
        }

        if (result.IsTimeout(out var timeout))
        {
            return timeout;
        }

        if (result.IsForbidden(out var forbidden))
        {
            return forbidden;
        }

        result.IsDeprecated(out var deprecated);

        return FlattenDeprecated(deprecated!);
    }

    /// <summary>
    /// Flattens the deprecated-outer-wrapping-inner-result case, merging deprecation metadata when the
    /// inner result is itself deprecated.
    /// </summary>
    private static Result<T> FlattenDeprecated<T>(Deprecated<Result<T>> outer)
    {
        var inner = outer.Value;

        if (inner.IsSuccess(out var value))
        {
            return new Deprecated<T>(value, outer.DeprecatedAfterUtc, outer.Message);
        }

        // Both layers deprecated the same value: the soonest sunset date is the one that binds.
        if (inner.IsDeprecated(out var innerDeprecated))
        {
            var deprecatedAfterUtc = outer.DeprecatedAfterUtc < innerDeprecated.DeprecatedAfterUtc
                ? outer.DeprecatedAfterUtc
                : innerDeprecated.DeprecatedAfterUtc;

            var messages = new[] { outer.Message, innerDeprecated.Message }.Where(m => !string.IsNullOrEmpty(m));
            var message = string.Join(" ", messages) is { Length: > 0 } joined ? joined : null;

            return new Deprecated<T>(innerDeprecated.Value, deprecatedAfterUtc, message);
        }

        if (inner.IsError(out var error))
        {
            return error;
        }

        if (inner.IsNotFound(out var notFound))
        {
            return notFound;
        }

        if (inner.IsConflict(out var conflict))
        {
            return conflict;
        }

        if (inner.IsGatewayError(out var gatewayError))
        {
            return gatewayError;
        }

        if (inner.IsTimeout(out var timeout))
        {
            return timeout;
        }

        inner.IsForbidden(out var forbidden);

        return forbidden;
    }
}
