---
name: brigade-net-mise-inserts
description: Build Mise INSERT commands, use query sources, and map returned rows.
---

# Mise inserts

Requires `brigade-net-mise-queries` (and setup); query-source/returned rows use `brigade-net-mise-rows`. Use engine command builder.

```csharp
var cmd = new SqlServerCommandBuilder()
    .InsertInto($"{Table.Table:raw}")
    .Columns($"id, name")
    .Values($"{id}, {name}")
    .Values($"{otherId}, {otherName}");
writer.ExecuteAsync(cmd, ct);

var fromQuery = new PostgreSqlCommandBuilder()
    .InsertInto($"{Table.Table:raw}")
    .Columns($"id, name")
    .FromQuery(query);

var returning = new PostgreSqlCommandBuilder()
    .InsertInto($"{Table.Table:raw}")
    .Columns($"name").Values($"{name}").Returning("id", "name");
writer.ReturningFirstOrNotFoundAsync<Row>(returning, ct);
```

Choose one source: one or more `Values(FormattableString)` rows, or `FromQuery(IQueryBuilder)`. Optional `Columns(FormattableString)` once. INSERT requires rows or query; target once. Holes bind values; `:raw` only constant SQL strings. `ExecuteAsync` returns affected count.

Returning: PostgreSQL/SQLite `.Returning(columns)`; MariaDB `.Returning(columns)` for INSERT (10.5+); SQL Server `.OutputInserted(columns)`; MySQL has no builder returning (`MySqlQueryBuilder.LastInsertId()` builds `SELECT LAST_INSERT_ID()`). Map output with `ReturningListAsync<Row>`, `ReturningFirstOrNotFoundAsync<Row>`, or `ExecuteScalarAsync<T>`. Generated row and DB null rules: `brigade-net-mise-rows`.

Returned list gives all rows; zero rows is an empty successful list. First gives first row, ignores later rows, or `NotFound` when none. Scalar gives first cell; no row or SQL NULL gives null.

`PrimaryKey`, `DatabaseGenerated`, `Computed`, `ExcludeFromInsert` are mapping metadata; builders do not auto-generate insert column lists. Leave generated/computed columns out explicitly. Execute in command handler with `[Provide] DbWriter` and UoW transaction.
