using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.Purchases.SearchSqliteV1;

[SqliteTable(null)]
public static partial class TreeTblSqlite
{
    [Column("id")]
    private static int Id { get; }
}
