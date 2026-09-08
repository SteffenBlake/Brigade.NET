namespace Brigade.Net.Core.Results;

/// <summary>
/// A failure indicating an upstream dependency returned an error.
/// </summary>
/// <param name="Message">An optional human-readable explanation.</param>
public sealed class GatewayError(string? Message = null) : FailureBase
{
    /// <summary>
    /// An optional human-readable explanation.
    /// </summary>
    public string? Message { get; } = Message;

    /// <inheritdoc />
    public override bool IsGatewayError(out GatewayError gatewayError)
    {
        gatewayError = this;
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => $"GatewayError {{ Message = {Message} }}";
}
