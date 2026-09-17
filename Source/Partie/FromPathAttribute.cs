namespace Brigade.Net.Partie;

/// <summary>Binds a request property from a path.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FromPathAttribute : Attribute
{
    /// <summary>The binding name, or null for the property's name.</summary>
    public string? Name { get; set; }
    /// <summary>An optional short name; ignored by the ASP.NET engine.</summary>
    public string? ShortName { get; set; }
}
