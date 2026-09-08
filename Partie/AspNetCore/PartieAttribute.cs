namespace Brigade.Net.Partie.AspNetCore;

/// <summary>
/// Adds a Partie type to a route's chain, in the order the attribute appears.
/// </summary>
/// <typeparam name="TPartie">The Partie type.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class PartieAttribute<TPartie> : Attribute;

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
