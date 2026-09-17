namespace Brigade.Net.Partie.Generator;

public sealed class RouteInputEmission(
    string typeName,
    string memberName,
    string bindingName,
    string source,
    string? runtimeTypeName = null
)
{
    public string TypeName { get; } = typeName;
    public string RuntimeTypeName { get; } = runtimeTypeName ?? typeName;
    public string MemberName { get; } = memberName;
    public string BindingName { get; } = bindingName;
    public string Source { get; } = source;
}
