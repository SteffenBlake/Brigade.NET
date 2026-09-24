using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Example.Domain.Purchases.SearchPostgreSqlV1;

[PostgreSqlTable(null)]
public static partial class TreeTblPostgreSql
{
    [Column("id")]
    private static int Id { get; }
}
