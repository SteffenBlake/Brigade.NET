namespace Brigade.Net.Partie.AspNetCore;

/// <summary>
/// Marks the Handler type that ends a route's chain.
/// </summary>
/// <typeparam name="THandler">The Handler type.</typeparam>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HandlerAttribute<THandler> : Attribute;

/// <summary>
/// Marks the static Handler type that ends a route's chain.
/// </summary>
/// <param name="handlerType">The Handler type.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HandlerAttribute(Type handlerType) : Attribute
{
	/// <summary>
	/// Gets the Handler type.
	/// </summary>
	public Type HandlerType { get; } = handlerType;
}
