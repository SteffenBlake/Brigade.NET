---
name: brigade-net-mise-queries
description: Build and execute Mise read queries with safe SQL interpolation and generated rows.
---

# Mise queries

Load `brigade-net-mise-setup` and `brigade-net-mise-rows`. Use active engine builder (`SqlServerQueryBuilder`, `PostgreSqlQueryBuilder`, `SqliteQueryBuilder`, `MySqlQueryBuilder`, `MariaDbQueryBuilder`) or `QueryBuilder(SqlDialect)`.

Every `FormattableString` hole is a bound parameter. `:raw` inserts only compile-time constant string; use generated `Table`, `*Col`, `*Join` constants. Never use runtime data as SQL syntax. Engine analyzer diagnoses nonconstant raw SQL and known MySQL/MariaDB `FULL JOIN`.

```csharp
new SqlServerQueryBuilder()
    .With("x", child).WithRecursive("tree", anchor, recursive)
    .Select($"{Table.IdCol:raw}").Select(child).Distinct()
    .From($"{Table.Table:raw}")
    .InnerJoin($"{Table.IdJoin:raw}")
    .LeftJoin(fragment).RightJoin(fragment).FullJoin(fragment)
    .CrossJoin(fragment).CrossJoin(child, "d")
    .Where($"id = {id}").WhereExists(child)
    .WhereIn($"id", values).WhereIn($"id", child)
    .GroupBy($"kind").Having($"COUNT(*) > {n}").OrderBy($"id DESC")
    .Union(child).UnionAll(child).Intersect(child).Except(child)
    .Offset(0).Limit(10).Behavior(CommandBehavior.SingleRow)
    .Compile();
```

`Where`/`Having` combine with AND; repeated clauses retain call order. Empty `WhereIn` list is false (`1 = 0`). Join fragment needs `ON`, cross join forbids it. Child builders must be Mise builders with same engine; cycles fail. `Sql(FormattableString)` is whole trusted SQL and cannot mix fluent clauses. `Compile()` gives fresh `CompiledSql(Text, Parameters, CommandType, Timeout, Behavior)`. Paging: SQL Server needs ORDER BY and positive limit; SQLite uses `LIMIT/OFFSET`; others use LIMIT/OFFSET. SQLite RIGHT/FULL needs SQLite 3.39+; MySQL/MariaDB lack FULL. SQL Server `.MaxRecursion(0..32767)`.

Constructors: `QueryBuilder()`, `QueryBuilder(SqlDialect)`. Dialect API: `Name`, `SupportsRightJoin`, `SupportsFullJoin`, `UsesRecursiveKeyword`, `RequiresOrderByForPaging`, `QuoteIdentifier(string)`, `AppendPaging(StringBuilder, int?, int?)`. MySQL: `MySqlQueryBuilder.LastInsertId()` returns query for `LAST_INSERT_ID()`.

```csharp
query.Compile() // CompiledSql: Text, Parameters, CommandType, Timeout, Behavior
reader.ListAsync<Row>(query, ct)
reader.StreamAsync<Row>(query, ct)
reader.FirstOrNotFoundAsync<Row>(query, ct)
reader.ScalarAsync<T>(query, ct)
reader.ExistsAsync(query, ct)
```

All terminals use `IQueryBuilder`; `Compile()` returns fresh parameter snapshot. See `brigade-net-mise-rows` for projections and mapping.
