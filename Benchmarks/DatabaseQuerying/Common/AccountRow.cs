using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Common;

[Mise]
public sealed partial record AccountRow
{
    [Column("id")]
    public required int Id { get; init; }

    [Column("name")]
    public required string Name { get; init; }
}
