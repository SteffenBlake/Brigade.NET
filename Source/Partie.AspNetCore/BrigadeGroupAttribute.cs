namespace Brigade.Net.Partie.AspNetCore;

/// <summary>
/// Marks a partial class as a Brigade route group with a shared path prefix.
/// </summary>
/// <param name="prefix">The path prefix applied to every route in the group.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BrigadeGroupAttribute(string prefix) : Attribute
{
    /// <summary>
    /// The path prefix applied to every route in the group.
    /// </summary>
    public string Prefix { get; } = prefix;
}
