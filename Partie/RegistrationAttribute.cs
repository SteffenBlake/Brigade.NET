namespace Brigade.Net.Partie;

/// <summary>Identifies the Partie or Provider represented by a generated attribute.</summary>
/// <param name="type">The registered type, which may be an open generic provider.</param>
/// <param name="isProvider">Whether the registration is a provider rather than an ordered Partie.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegistrationAttribute(Type type, bool isProvider = false) : Attribute
{
    /// <summary>Gets the registered type.</summary>
    public Type Type { get; } = type;
    /// <summary>Gets whether this is a provider registration.</summary>
    public bool IsProvider { get; } = isProvider;
}
