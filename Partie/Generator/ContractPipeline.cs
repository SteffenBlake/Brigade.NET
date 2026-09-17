using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

// TODO: Maybe we need to update the skill file?
// Pretty sure most cases like these can be records instead of classes, no?
// Check before assuming though, make sure it compiles and runs
// If it works, THEN update the skill file and fix

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
