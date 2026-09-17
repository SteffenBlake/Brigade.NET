namespace Brigade.Net.Partie;

/// <summary>Groups routes under a shared path. Nested groups contribute components in outer-to-inner order.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BrigadeGroupAttribute(params string[] path) : Attribute
{
    /// <summary>Gets this group's local path components, interpreted by the engine.</summary>
    public IReadOnlyList<string> Path { get; } = Array.AsReadOnly(path.ToArray());
}
