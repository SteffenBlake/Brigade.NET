namespace Brigade.Net.Partie.Generator;

public sealed class RoutePolicyEmission(string policyTypeName, string methodName)
{
    public string PolicyTypeName { get; } = policyTypeName;
    public string MethodName { get; } = methodName;
}