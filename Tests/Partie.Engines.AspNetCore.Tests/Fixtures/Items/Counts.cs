
namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items;

public sealed class Counts
{
    public System.Collections.Concurrent.ConcurrentQueue<
        (int Id, string Text, Guid Scope, bool SameCancellation)
    > Observed { get; } = new();

    public int ProviderRuns;
    public int HandlerRuns;
}
