namespace Brigade.Net.Core.Results;

/// <summary>
/// A failure indicating the caller is not permitted to perform the requested operation.
/// </summary>
public sealed class Forbidden : FailureBase
{
    /// <inheritdoc />
    public override bool IsForbidden(out Forbidden forbidden)
    {
        forbidden = this;
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => "Forbidden";
}
