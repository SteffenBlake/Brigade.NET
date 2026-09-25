using System.Linq;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal sealed record RouteInputBinding(string Source, string Name)
{
    public static RouteInputBinding[] Read(IParameterSymbol parameter) => parameter
        .GetAttributes()
        .Where(attribute =>
            attribute.AttributeClass?.ContainingNamespace.ToDisplayString() == "Brigade.Net.Partie"
        )
        .Where(attribute =>
            attribute.AttributeClass!.Name
                is "FromRouteAttribute"
                    or "FromQueryAttribute"
                    or "FromBodyAttribute"
                    or "FromServicesAttribute"
        )
        .Select(attribute =>
        {
            var attributeName = attribute.AttributeClass!.Name;
            var source = attributeName == "FromServicesAttribute"
                ? "Service"
                : attributeName.Substring(4).Replace("Attribute", "");
            var name = attribute.ConstructorArguments.FirstOrDefault().Value as string ?? parameter.Name;

            return new RouteInputBinding(source, name);
        })
        .ToArray();
}
