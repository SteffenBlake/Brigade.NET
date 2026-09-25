namespace Brigade.Net.Partie;

/// <summary>
/// Obtains a constructor argument from the route's provided values.
/// </summary>
/// <remarks>
/// Matching Sources may be registered before or after the consumer. A Source may be
/// a Provider, Partie, or decorated value. A single value selects the latest matching
/// Source. IEnumerable&lt;T&gt; collects all matching Sources.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ProvideAttribute : Attribute
{
}
