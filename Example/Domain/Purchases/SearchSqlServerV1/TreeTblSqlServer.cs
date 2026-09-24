using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Example.Domain.Purchases.SearchSqlServerV1;

[SqlServerTable(null)]
public static partial class TreeTblSqlServer
{
    [Column("id")]
    private static int Id { get; }
}
