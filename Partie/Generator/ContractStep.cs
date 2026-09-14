namespace Brigade.Net.Partie.Generator;

internal sealed class ContractStep(
    string typeName,
    string providedType,
    string contextType,
    string context,
    string valueName
)
{
    public string TypeName { get; } = typeName;
    public string ProvidedType { get; } = providedType;
    public string ContextType { get; } = contextType;
    public string Context { get; } = context;
    public string ValueName { get; } = valueName;
}
