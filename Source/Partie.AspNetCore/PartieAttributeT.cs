namespace Brigade.Net.Partie.AspNetCore;

/// <summary>
/// Adds a Partie type to a route's chain, in the order the attribute appears.
/// </summary>
/// <typeparam name="TPartie">The Partie type.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class PartieAttribute<TPartie> : Attribute
{
}

