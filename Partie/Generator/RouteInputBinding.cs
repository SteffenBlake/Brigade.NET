using System.Linq;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal sealed class RouteInputBinding(string source, string name)
{
    public string Source { get; } = source;
    public string Name { get; } = name;

    public static RouteInputBinding[] Read(IParameterSymbol parameter) => parameter.GetAttributes()
        .Where(attribute => attribute.AttributeClass?.ContainingNamespace.ToDisplayString() == "Brigade.Net.Partie")
        .Where(attribute => attribute.AttributeClass!.Name is "FromRouteAttribute" or "FromQueryAttribute" or "FromBodyAttribute" or "FromServicesAttribute")
        .Select(attribute => new RouteInputBinding(
            attribute.AttributeClass!.Name == "FromServicesAttribute" ? "Service" : attribute.AttributeClass.Name.Substring(4).Replace("Attribute", ""),
            attribute.ConstructorArguments.FirstOrDefault().Value as string ?? parameter.Name
        )).ToArray();
}