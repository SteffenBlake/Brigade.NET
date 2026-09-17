using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator;

internal sealed record ContractPipeline(
    string ResultType,
    RequestEmission Request,
    ImmutableArray<RouteInputEmission> Inputs,
    string Body
);
