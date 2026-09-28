namespace Brigade.Net.Partie;

/// <summary>Identifies the declaration represented by a generated bundle attribute.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BundleRegistrationAttribute(Type type) : Attribute
{
    /// <summary>Gets the bundle whose registrations are expanded at this position.</summary>
    public Type Type { get; } = type;
}
