using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteEmission(
    string name,
    string pattern,
    string operation,
    string descriptorExpression,
    string inputTypeName,
    ImmutableArray<RouteInputEmission> inputs,
    ImmutableArray<RoutePolicyEmission> policies = default,
    ImmutableArray<string> policyFunctions = default
)
{
    public string Name { get; } = name;
    public string Pattern { get; } = pattern;
    public string Operation { get; } = operation;
    public string DescriptorExpression { get; } = descriptorExpression;
    public string InputTypeName { get; } = inputTypeName;
    public ImmutableArray<RouteInputEmission> Inputs { get; } = inputs;
    public ImmutableArray<RoutePolicyEmission> Policies { get; } = policies;
    public ImmutableArray<string> PolicyFunctions { get; } = policyFunctions;
}