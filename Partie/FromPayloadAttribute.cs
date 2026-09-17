namespace Brigade.Net.Partie;

/// <summary>Binds a property from a request payload.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FromPayloadAttribute : Attribute
{
    /// <summary>The payload encoding; JSON by default.</summary>
    public PayloadFormat Format { get; set; } = PayloadFormat.Json;
}
