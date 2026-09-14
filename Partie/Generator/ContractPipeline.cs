using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

internal sealed class ContractPipeline(
    string resultType,
    RequestEmission request,
    ImmutableArray<RouteInputEmission> inputs,
    string body
)
{
    public string ResultType { get; } = resultType;
    public RequestEmission Request { get; } = request;
    public ImmutableArray<RouteInputEmission> Inputs { get; } = inputs;
    public string Body { get; } = body;
}
