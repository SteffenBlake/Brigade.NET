namespace Brigade.Net.Core.Results;

/// <summary>
/// A failure indicating an upstream dependency did not respond in time.
/// </summary>
/// <param name="Message">An optional human-readable explanation.</param>
public sealed class TimeoutResult(string? Message = null) : FailureBase
{
    /// <summary>
    /// An optional human-readable explanation.
    /// </summary>
    public string? Message { get; } = Message;

    /// <inheritdoc />
    public override bool IsTimeout(out TimeoutResult timeout)
    {
        timeout = this;
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => $"TimeoutResult {{ Message = {Message} }}";
}
