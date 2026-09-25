namespace Brigade.Net.Partie;

/// <summary>
/// Binds a value directly from engine services rather than a Brigade provider.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromServicesAttribute : Attribute
{
}
