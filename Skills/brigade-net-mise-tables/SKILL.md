---
name: brigade-net-mise-tables
description: Define Mise mapped tables, columns, aliases, qualifiers, and generated relationships.
---

# Mise tables

Load `brigade-net-mise-setup`; choose the same engine for related tables. Table target must be `static partial class`; containing types partial too.

```csharp
[SqlServerTable("purchases")]
[Schema("sales")]
[Alias("Purchase")]
public static partial class PurchaseTbl
{
    [Column("id"), PrimaryKey] private static int Id { get; }
    [Column("buyer_id"), Relationship(AccountTbl.IdCol)] private static int BuyerId { get; }
}
```

Table attrs: `[SqlServerTable("t")]`, `[PostgreSqlTable("t")]`, `[SqliteTable("t")]`, `[MySqlTable("t")]`, `[MariaDbTable("t")]`. Pass `null` for virtual name inference: take type name, cut at its last `Tbl` when not first char, then lowercase first char (`TreeTblSqlServer` → `tree`; `Purchase` → `purchase`). Virtual generates `Name`, no DDL. Qualifiers: SQL Server/PostgreSQL `[Schema("s")]`; MySQL/MariaDB `[Database("d")]`.

Map every static property with `[Column("db_name")]`. Add repeatable `[Alias("a")]`. Alias generated nested class exposes `.Table`, `.XCol`, and joins. `[Relationship(Target.XCol)]` belongs on source property; target must be same-engine table and target col must be mapped. Generated join name is `{Property}Join`; target aliases append alias name; reverse joins arise from target aliases.

Generated members: `Table` is qualified and quoted; each `{Property}Col`; aliases contain same members. Identifiers are quoted by engine: SQL Server `[]`, PostgreSQL/SQLite `""`, MySQL/MariaDB backticks. Do not hand-build their quoted constants.

Generator checks: table names/column names/qualifiers/aliases nonempty; column and alias IDs unique under engine comparer; class partial/static; no generated name collision (`Table`, `{Property}Col`, joins, aliases); one table engine per type. SQL Server, MySQL, MariaDB, SQLite compare identifiers case-insensitively; PostgreSQL case-sensitively.

Column metadata: `[PrimaryKey(position = 0)]` uses unique nonnegative zero-based order. `[DatabaseGenerated]` means generated on insert; `[Computed]` means DB-computed/read-only; cannot combine. `[ExcludeFromInsert]`, `[ExcludeFromUpdate]` describe write exclusion metadata. These attrs do not generate CRUD builders or DDL.
