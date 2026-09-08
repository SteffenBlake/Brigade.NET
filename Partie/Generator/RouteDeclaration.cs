namespace Brigade.Net.Partie.Generator;

public sealed class RouteDeclaration(string pattern, string operation)
{
    public string Pattern { get; } = pattern;
    public string Operation { get; } = operation;
}