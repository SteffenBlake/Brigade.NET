using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteEmission(
    string name,
    string pattern,
    string operation,
    string descriptorExpression,
    string inputTypeName,
    ImmutableArray<RouteInputEmission> inputs
)
{
    public string Name { get; } = name;
    public string Pattern { get; } = pattern;
    public string Operation { get; } = operation;
    public string DescriptorExpression { get; } = descriptorExpression;
    public string InputTypeName { get; } = inputTypeName;
    public ImmutableArray<RouteInputEmission> Inputs { get; } = inputs;
}

public sealed class RouteInputEmission(string typeName, string memberName, string bindingName, string source)
{
    public string TypeName { get; } = typeName;
    public string MemberName { get; } = memberName;
    public string BindingName { get; } = bindingName;
    public string Source { get; } = source;
}