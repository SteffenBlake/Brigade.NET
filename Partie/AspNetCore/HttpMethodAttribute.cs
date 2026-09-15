namespace Brigade.Net.Partie.Engines.AspNetCore;

/// <summary>
/// Base type for attributes that bind a route method to an HTTP verb and path.
/// </summary>
/// <param name="path">The route path, relative to the enclosing group.</param>
public abstract class HttpMethodAttribute(string path) : Attribute
{
    /// <summary>
    /// The route path, relative to the enclosing group.
    /// </summary>
    public string Path { get; } = path;
}
