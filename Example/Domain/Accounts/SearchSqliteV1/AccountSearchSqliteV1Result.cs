using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.Accounts.SearchSqliteV1;

[Mise]
public sealed partial record AccountSearchSqliteV1Result
{
    [Column("id")]
    public required int Id { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("parent_id")]
    public required int? ParentId { get; init; }

    [Column("group")]
    public required string? Group { get; init; }

    [Column("note")]
    public required string? Note { get; init; }
}
