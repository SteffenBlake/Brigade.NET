namespace Brigade.Net.Partie;

/// <summary>
/// Binds a value from a request payload or command input.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromBodyAttribute : Attribute
{
}
