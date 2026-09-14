namespace Brigade.Net.Partie.Generator;

public sealed class RequestPropertyEmission(
    string name,
    string typeName,
    string source,
    string? bindingName,
    string? shortName,
    bool required,
    string metadata
)
{
    public string Name { get; } = name;
    public string TypeName { get; } = typeName;
    public string Source { get; } = source;
    public string? BindingName { get; } = bindingName;
    public string? ShortName { get; } = shortName;
    public bool Required { get; } = required;
    public string Metadata { get; } = metadata;
}
