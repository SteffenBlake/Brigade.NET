namespace Brigade.Net.Partie.AspNetCore;

/// <summary>
/// Registers a Partie type as a provider available to every route in the group or route it is
/// applied to. A provider may be an open generic type.
/// </summary>
/// <param name="providerType">The provider Partie type, which may be an open generic type.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProviderAttribute(Type providerType) : Attribute
{
    /// <summary>
    /// The provider Partie type, which may be an open generic type.
    /// </summary>
    public Type ProviderType { get; } = providerType;
}
