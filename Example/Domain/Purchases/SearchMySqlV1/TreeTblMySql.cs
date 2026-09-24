using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Example.Domain.Purchases.SearchMySqlV1;

[MySqlTable(null)]
public static partial class TreeTblMySql
{
    [Column("id")]
    private static int Id { get; }
}
