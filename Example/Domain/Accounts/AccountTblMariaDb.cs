using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Example.Domain.Accounts;

[MariaDbTable("accounts")]
[Alias("Buyer")]
[Alias("Seller")]
public static partial class AccountTblMariaDb
{
    [Column("id")]
    private static int Id { get; }

    [Column("name")]
    private static string Name => string.Empty;

    [Column("parent_id")]
    private static int? ParentId { get; }

    [Column("group")]
    private static string? Group { get; }

    [Column("note")]
    private static string? Note { get; }
}
