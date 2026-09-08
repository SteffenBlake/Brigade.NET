namespace Brigade.Net.Partie.AspNetCore;

/// <summary>
/// Base type for attributes that bind a route method to an HTTP verb and path.
/// </summary>
/// <param name="path">The route path, relative to the enclosing group's prefix.</param>
public abstract class HttpMethodAttribute(string path) : Attribute
{
    /// <summary>
    /// The route path, relative to the enclosing group's prefix.
    /// </summary>
    public string Path { get; } = path;
}

/// <summary>
/// Binds a route method to an HTTP GET request.
/// </summary>
/// <param name="path">The route path, relative to the enclosing group's prefix.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GetAttribute(string path) : HttpMethodAttribute(path);

/// <summary>
/// Binds a route method to an HTTP POST request.
/// </summary>
/// <param name="path">The route path, relative to the enclosing group's prefix.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostAttribute(string path) : HttpMethodAttribute(path);

/// <summary>
/// Binds a route method to an HTTP PUT request.
/// </summary>
/// <param name="path">The route path, relative to the enclosing group's prefix.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PutAttribute(string path) : HttpMethodAttribute(path);

/// <summary>
/// Binds a route method to an HTTP DELETE request.
/// </summary>
/// <param name="path">The route path, relative to the enclosing group's prefix.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DeleteAttribute(string path) : HttpMethodAttribute(path);

/// <summary>
/// Binds a route method to an HTTP PATCH request.
/// </summary>
/// <param name="path">The route path, relative to the enclosing group's prefix.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PatchAttribute(string path) : HttpMethodAttribute(path);
