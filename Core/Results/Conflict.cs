namespace Brigade.Net.Core.Results;

/// <summary>
/// A failure indicating the request conflicts with the current state of the resource.
/// </summary>
/// <param name="Message">An optional human-readable explanation.</param>
public sealed class Conflict(string? Message = null) : FailureBase
{
    /// <summary>
    /// An optional human-readable explanation.
    /// </summary>
    public string? Message { get; } = Message;

    /// <inheritdoc />
    public override bool IsConflict(out Conflict conflict)
    {
        conflict = this;
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => $"Conflict {{ Message = {Message} }}";
}
