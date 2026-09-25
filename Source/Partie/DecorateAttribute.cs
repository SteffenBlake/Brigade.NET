namespace Brigade.Net.Partie;

/// <summary>
/// Obtains values provided before the consumer in route order.
/// </summary>
/// <remarks>
/// A single value selects the latest earlier value. IEnumerable&lt;T&gt; collects all
/// earlier values in route order.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class DecorateAttribute : Attribute
{
}
