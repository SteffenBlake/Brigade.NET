using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Example.Domain.Purchases.SearchMariaDbV1;

[MariaDbTable(null)]
public static partial class TreeTblMariaDb
{
    [Column("id")]
    private static int Id { get; }
}
