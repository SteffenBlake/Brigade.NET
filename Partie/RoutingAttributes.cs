namespace Brigade.Net.Partie;

/// <summary>Groups routes under a shared pattern prefix.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BrigadeGroupAttribute(string prefix) : Attribute
{
    /// <summary>Gets the shared pattern prefix.</summary>
    public string Prefix { get; } = prefix;
}

/// <summary>Declares a transport-neutral route and engine-defined operation.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RouteAttribute(string pattern, string operation) : Attribute
{
    /// <summary>Gets the route pattern.</summary>
    public string Pattern { get; } = pattern;
    /// <summary>Gets the engine-defined operation.</summary>
    public string Operation { get; } = operation;
}

/// <summary>Chooses the Handler type.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HandlerAttribute(Type handlerType) : Attribute
{
    /// <summary>Gets the Handler type.</summary>
    public Type HandlerType { get; } = handlerType;
}

/// <summary>Adds a fixed step to the ordered route chain.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class PartieAttribute(Type partieType) : Attribute
{
    /// <summary>Gets the step type.</summary>
    public Type PartieType { get; } = partieType;
}

/// <summary>Registers a provider, including open generic provider types.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProviderAttribute(Type providerType) : Attribute
{
    /// <summary>Gets the provider type.</summary>
    public Type ProviderType { get; } = providerType;
}

/// <summary>Binds a value from a route path or command position.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromRouteAttribute(string? name = null) : Attribute
{
    /// <summary>Gets the binding name, or null to use the parameter name.</summary>
    public string? Name { get; } = name;
}

/// <summary>Binds a value from a query or named command option.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromQueryAttribute(string? name = null) : Attribute
{
    /// <summary>Gets the binding name, or null to use the parameter name.</summary>
    public string? Name { get; } = name;
}

/// <summary>Binds a value from a request payload or command input.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromBodyAttribute : Attribute;

/// <summary>Binds a value directly from engine services rather than a Brigade provider.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromServicesAttribute : Attribute;