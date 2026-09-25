using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

public sealed record RouteEmission(
    string name,
    ImmutableArray<string> path,
    string operation,
    string descriptorExpression,
    string inputTypeName,
    ImmutableArray<RouteInputEmission> inputs,
    ImmutableArray<RoutePolicyEmission> policies = default,
    ImmutableArray<string> policyFunctions = default,
    RequestEmission? request = null,
    ImmutableArray<RouteGroupEmission> groups = default,
    ImmutableArray<string> localPath = default
)
{
    public RequestEmission? Request { get; } = request;

    public string Name { get; } = name;

    /// <summary>Gets the full path, from outer groups through the route, without transport formatting.</summary>
    public ImmutableArray<string> Path { get; } = path;

    public ImmutableArray<RouteGroupEmission> Groups { get; } = groups;

    public ImmutableArray<string> LocalPath { get; } = localPath;

    public string Operation { get; } = operation;

    public string DescriptorExpression { get; } = descriptorExpression;

    public string InputTypeName { get; } = inputTypeName;

    public ImmutableArray<RouteInputEmission> Inputs { get; } = inputs;

    public ImmutableArray<RoutePolicyEmission> Policies { get; } = policies;

    public ImmutableArray<string> PolicyFunctions { get; } = policyFunctions;
}
