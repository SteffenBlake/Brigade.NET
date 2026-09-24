using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Example.Domain.Accounts;

[MySqlTable("accounts")]
[MySqlRow]
[MiseAlias("BuyerAlias")]
[MiseAlias("SellerAlias")]
public partial class AccountTblMySql
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("name")]
    public string Name { get; set; } = string.Empty;

    [MiseColumn("parent_id")]
    public int? ParentId { get; set; }

    [MiseColumn("group")]
    public string? Group { get; set; }

    [MiseColumn("note")]
    public string? Note { get; set; }
}
