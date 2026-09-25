namespace Brigade.Net.Partie.Generator;

public sealed record RoutePolicyEmission(
    string PolicyTypeName,
    string MethodName,
    bool GenericMethod = false
);
