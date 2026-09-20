namespace Brigade.Net.Partie.Generator;

public sealed class RoutePolicyEmission(
    string policyTypeName,
    string methodName,
    bool genericMethod = false
)
{
    public string PolicyTypeName { get; } = policyTypeName;
    public string MethodName { get; } = methodName;
    public bool GenericMethod { get; } = genericMethod;
}
