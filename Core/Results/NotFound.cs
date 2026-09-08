namespace Brigade.Net.Core.Results;

/// <summary>
/// A failure indicating the requested resource does not exist.
/// </summary>
/// <param name="Message">An optional human-readable explanation.</param>
public sealed class NotFound(string? Message = null) : FailureBase
{
    /// <summary>
    /// An optional human-readable explanation.
    /// </summary>
    public string? Message { get; } = Message;

    /// <inheritdoc />
    public override bool IsNotFound(out NotFound notFound)
    {
        notFound = this;
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => $"NotFound {{ Message = {Message} }}";
}
