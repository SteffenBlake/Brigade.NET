namespace Brigade.Net.Partie;

/// <summary>
/// Declares a transport-neutral route bound to a handler.
/// </summary>
/// <typeparam name="THandler">The route handler.</typeparam>
/// <param name="path">The local path component.</param>
/// <param name="operation">The engine-defined operation.</param>
public sealed class RouteAttribute<THandler>(string path, string operation)
    : RouteAttribute(path, operation)
{
}
