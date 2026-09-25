namespace Brigade.Net.Partie.AspNetCore;

/// <summary>
/// Adds a static Partie type to a route's ordered chain.
/// </summary>
/// <param name="partieType">The Partie type.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class PartieAttribute(Type partieType) : Attribute
{
    /// <summary>
    /// Gets the Partie type.
    /// </summary>
    public Type PartieType { get; } = partieType;
}
