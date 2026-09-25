namespace Brigade.Net.Partie;

/// <summary>
/// Registers a provider, including open generic provider types.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProviderAttribute(Type providerType) : Attribute
{
    /// <summary>Gets the provider type.</summary>
    public Type ProviderType { get; } = providerType;
}
