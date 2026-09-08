namespace Brigade.Net.Core.Results;

/// <summary>
/// Base for every non-generic failure payload (<see cref="Error" />, <see cref="NotFound" />,
/// <see cref="Conflict" />, <see cref="Forbidden" />, <see cref="GatewayError" />, <see cref="TimeoutResult" />).
/// Lets <see cref="Failure{T, TFailure}" /> delegate straight to the wrapped value instead
/// of switching on which failure type it actually holds.
/// </summary>
public abstract class FailureBase
{
    /// <summary>
    /// Attempts to read this instance as an <see cref="Results.Error" />.
    /// </summary>
    /// <param name="error">The error, if this instance is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this instance is an <see cref="Results.Error" />.</returns>
    public virtual bool IsError(out Error error)
    {
        error = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this instance as a <see cref="Results.NotFound" />.
    /// </summary>
    /// <param name="notFound">The value, if this instance is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this instance is a <see cref="Results.NotFound" />.</returns>
    public virtual bool IsNotFound(out NotFound notFound)
    {
        notFound = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this instance as a <see cref="Results.Conflict" />.
    /// </summary>
    /// <param name="conflict">The value, if this instance is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this instance is a <see cref="Results.Conflict" />.</returns>
    public virtual bool IsConflict(out Conflict conflict)
    {
        conflict = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this instance as a <see cref="Results.Forbidden" />.
    /// </summary>
    /// <param name="forbidden">The value, if this instance is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this instance is a <see cref="Results.Forbidden" />.</returns>
    public virtual bool IsForbidden(out Forbidden forbidden)
    {
        forbidden = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this instance as a <see cref="Results.GatewayError" />.
    /// </summary>
    /// <param name="gatewayError">The value, if this instance is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this instance is a <see cref="Results.GatewayError" />.</returns>
    public virtual bool IsGatewayError(out GatewayError gatewayError)
    {
        gatewayError = default!;
        return false;
    }

    /// <summary>
    /// Attempts to read this instance as a <see cref="Results.TimeoutResult" />.
    /// </summary>
    /// <param name="timeout">The value, if this instance is one; otherwise the default value.</param>
    /// <returns><see langword="true" /> if this instance is a <see cref="Results.TimeoutResult" />.</returns>
    public virtual bool IsTimeout(out TimeoutResult timeout)
    {
        timeout = default!;
        return false;
    }
}
