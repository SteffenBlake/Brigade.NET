using System;

namespace Brigade.Net.Partie.Engines.AspNetCore;

/// <summary>Binds a static route policy class that configures the fluent API of route registration.</summary>
/// <remarks>
/// The policy class must contain static methods with the pattern:
/// - <c>Query&lt;TParams&gt;(RouteHandlerBuilder route)</c> for query operations
/// - <c>Command&lt;TParams, TBody&gt;(RouteHandlerBuilder route)</c> for command operations
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RoutePolicyAttribute(Type policyType) : Attribute
{
    /// <summary>Gets the route policy type.</summary>
    public Type PolicyType { get; } = policyType;
}
