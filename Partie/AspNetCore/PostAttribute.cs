namespace Brigade.Net.Partie.Engines.AspNetCore;

/// <summary>Binds a route method to an HTTP POST request.</summary>
/// <param name="path">The route path, relative to the enclosing group; empty uses the group path.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostAttribute(string path = "") : HttpMethodAttribute(path);
