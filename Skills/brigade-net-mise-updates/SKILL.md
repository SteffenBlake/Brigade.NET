---
name: brigade-net-mise-updates
description: Build Mise UPDATE and DELETE commands, execute them, and map returned rows.
---

# Mise updates

Requires `brigade-net-mise-queries` (and setup); returned-row commands also use `brigade-net-mise-rows`. Use engine command builder: `SqlServerCommandBuilder`, `PostgreSqlCommandBuilder`, `SqliteCommandBuilder`, `MySqlCommandBuilder`, `MariaDbCommandBuilder`.

```csharp
var cmd = new SqlServerCommandBuilder()
    .Update($"{Table.Table:raw}")
    .Set($"name = {name}")
    .Set("stamp", scalarQuery)
    .Where($"{Table.IdCol:raw} = {id}");
writer.ExecuteAsync(cmd, ct); // Result<int>, affected rows incl zero

var delete = new SqliteCommandBuilder()
    .DeleteFrom($"{Table.Table:raw}").Where($"id = {id}");
writer.ExecuteAsync(delete, ct);
```

Normal holes bind values; `:raw` only constant SQL strings. `Set(FormattableString)` adds assignment; `Set(string column, IQueryBuilder)` uses scalar subquery. `Where` ANDs predicates. One target only; UPDATE needs SET. Parameters keep SQL order.

Returning: PostgreSQL/SQLite `.Returning("id", "name")` works UPDATE/DELETE; SQL Server `.OutputInserted("id", "name")` works UPDATE; MySQL has no builder returning; MariaDB `.Returning(...)` supports DELETE only (not UPDATE). PostgreSQL/SQLite/MariaDB support INSERT returning too; see insert skill. Use:

```csharp
writer.ReturningListAsync<Row>(cmd, ct)
writer.ReturningFirstOrNotFoundAsync<Row>(cmd, ct)
writer.ExecuteScalarAsync<T>(cmd, ct)
```

List returns all rows; no rows is an empty successful list. First returns first row, ignores later rows, or `NotFound` if none. Scalar returns first cell; no row or SQL NULL gives null.

Write in command handler with `[Provide] DbWriter`; `[UnitOfWorkPartie]` owns transaction. For custom SQL use `.Sql($"UPDATE ... {value}")`; values remain bound. See `brigade-net-partie` for handler/UoW contracts.

Command API scope: `InsertInto(FormattableString)` starts INSERT; `Update(FormattableString)` starts UPDATE; `DeleteFrom(FormattableString)` starts DELETE. `Set(FormattableString)` and `Set(string, IQueryBuilder)` are UPDATE only. `Where(FormattableString)` is UPDATE/DELETE only. `Columns(FormattableString)`, `Values(FormattableString)`, `FromQuery(IQueryBuilder)` are INSERT only; choose VALUES or query source. `Sql(FormattableString)` and `Procedure(string)` each choose standalone custom SQL or procedure mode. `ProcedureParameter(string, object?, DbType? = null)` is procedure only. `Timeout(int)` and `Compile()` apply to all command kinds. Invalid mixes fail at compile.
