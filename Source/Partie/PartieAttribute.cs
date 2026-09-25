namespace Brigade.Net.Partie;

/// <summary>
/// Adds a fixed step to the ordered route chain.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PartieAttribute(Type partieType) : Attribute
{
    /// <summary>Gets the step type.</summary>
    public Type PartieType { get; } = partieType;
}
