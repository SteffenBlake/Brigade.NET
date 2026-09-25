namespace Brigade.Net.Partie;

/// <summary>
/// Binds a property from a request payload.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FromPayloadAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the payload encoding. The default is JSON.
    /// </summary>
    public PayloadFormat Format { get; set; } = PayloadFormat.Json;
}
