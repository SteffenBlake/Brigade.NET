using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator.Tests;

internal sealed class CapturingGenerator(
    Action<RouteEmission> capture,
    Action<RouteGroupEmission>? captureGroup = null,
    Func<AttributeData, RouteDeclaration?>? discoverRoute = null,
    Func<IMethodSymbol, bool>? discoverPolicyFunctions = null
) : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) =>
        BrigadeGeneratorCore.Initialize(
            context,
            route =>
            {
                capture(route);
                return "";
            },
            discoverRoute: discoverRoute,
            discoverPolicyFunctions: discoverPolicyFunctions,
            emitGroup: group =>
            {
                captureGroup?.Invoke(group);
                return "";
            }
        );
}
