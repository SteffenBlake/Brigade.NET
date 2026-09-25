namespace Brigade.Net.Partie.Generator;

public sealed record RequestPropertyEmission(
    string Name,
    string TypeName,
    string Source,
    string? BindingName,
    string? ShortName,
    bool Required,
    string Metadata
);
