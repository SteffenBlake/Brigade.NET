namespace Brigade.Net.Partie.Generator;

internal sealed record ContractStep(
    string TypeName,
    string ProvidedType,
    string ContextType,
    string Context,
    string ValueName,
    bool Provider,
    int Position
);
