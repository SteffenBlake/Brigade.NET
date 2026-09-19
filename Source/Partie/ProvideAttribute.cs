namespace Brigade.Net.Partie;

/// <summary>Obtains a constructor argument from the route's provided values.</summary>
/// <remarks>
/// A single value selects the latest matching Provider or Partie registered before
/// the consumer. IEnumerable&lt;T&gt; collects all earlier matching values in registration
/// order. Provider contexts resolve at their registration position.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ProvideAttribute : Attribute;
