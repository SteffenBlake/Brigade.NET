using System;

namespace Brigade.Net.Partie.Engines.AspNetCore;

/// <summary>Identifies the route policy represented by a generated attribute.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class RoutePolicyAttribute(Type policyType) : Attribute
{
    /// <summary>Gets the route policy type.</summary>
    public Type PolicyType { get; } = policyType;
}
