---
name: brigade-net-mise-rows
description: Define Mise generated result row types and satisfy column materialization rules.
---

# Mise rows

Load `brigade-net-mise-setup`; select row attribute from active engine namespace: `[Mise]` in `SqlServer`, `PostgreSQL`, `SQLite`, `MySQL`, or `MariaDb`.

```csharp
[Mise]
public sealed partial record PurchaseRow(int Id, string Name);
```

`[Mise]` emits `IRow<T>`, `BindOrdinals(DbDataReader)`, and `Materialize(DbDataReader, ReadOnlySpan<int>)`. Row and containing types must be partial. Row must not be static, abstract, or ref-like. Exactly one accessible instance ctor must have parameters matching mapped properties by case-insensitive name and exact type. Other mapped properties need writable setters. Required members/nullability still apply.

Every declared and inherited instance property except indexers is mapped; each needs `[Column("db_name")]`. Keep non-result properties out of row types.

Select every mapped property once under its DB column name as SQL result name; result-column matching is exact on PostgreSQL and case-insensitive on SQL Server, SQLite, MySQL, and MariaDB. Missing or duplicate matches throw `InvalidMappingException`. SQL aliases can provide names. DB null maps to null for nullable properties; non-nullable properties throw `MappingException`.

All reader/writer row APIs require `T : IRow<T>`:

```csharp
reader.ListAsync<Row>(query, ct)
reader.StreamAsync<Row>(query, ct)
reader.FirstOrNotFoundAsync<Row>(query, ct)
writer.ReturningListAsync<Row>(command, ct)
writer.ReturningFirstOrNotFoundAsync<Row>(command, ct)
```

See `brigade-net-mise-queries` for projections and execution; `brigade-net-mise-inserts` / `brigade-net-mise-updates` for returned rows.
