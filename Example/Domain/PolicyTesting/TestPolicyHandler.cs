using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.PolicyTesting;

/// <summary>Simple handler for policy examples.</summary>
public sealed class TestPolicyHandler : IQueryHandler<Unit, string, Unit>
{
    public static Task<Result<string>> RunAsync(
        Unit ctx,
        Unit query,
        CancellationToken ct
    ) => Task.FromResult<Result<string>>("success");
}
