# 05 Query API

Depends: 02, 04.

- [ ] Define `IQueryBuilder` as the execution boundary accepted by `DbReader` and `DbWriter`. Core supplies `QueryBuilder`; engine packages may expose specialized .NET 10 builders with extra fluent operations.
- [ ] Build SQL from SQL-shaped fluent methods and `FormattableString` input. The API includes operations such as `With`, `WithRecursive`, `Select`, `From`, joins, `Where`, grouping, ordering, and writes; it does not mimic LINQ.
- [ ] Parse composite formatting correctly, including escaped braces, alignment, repeated arguments, and format strings. Reject unknown Mise formats instead of passing them to `ToString`.
- [ ] Parameterize each ordinary interpolation. Generate stable, collision-free names after nested fragments merge. Create parameters through the selected provider factory and represent `null` as `DBNull.Value`.
- [ ] Treat format `raw` as the only interpolation bypass. The Roslyn analyzer requires the interpolated expression to be a compile-time constant string. Runtime values, including ordinary string variables, cannot use `:raw`.
- [ ] Define `MiseCommand` as an immutable command snapshot: command text, ordered parameter specifications, `CommandType`, timeout, and supported engine-neutral behavior flags. Building twice must not share mutable parameters.
- [ ] Support the common surface: WITH and recursive WITH; SELECT/DISTINCT; FROM; generated aliases; supported joins; subqueries; WHERE; GROUP BY; HAVING; ORDER BY; paging through engine rendering; set operations; INSERT values/select; UPDATE; DELETE; scalar/existence; stored procedure; and custom SQL.
- [ ] `InnerJoin`, `LeftJoin`, `RightJoin`, and `FullJoin` take one generated relationship const string that already contains the target and `ON` predicate. Alias relationship constants rewrite source qualification. `CrossJoin` takes only a table, alias, or child builder because it has no `ON` predicate.
- [ ] Fluent operations that contain a query accept `IQueryBuilder` children, including CTEs, recursive CTE anchor/recursive members, derived tables, correlated subqueries, existence checks, and set operations.
- [ ] Render recursive CTEs per engine. PostgreSQL, SQLite, MySQL, and MariaDB use `WITH RECURSIVE`; SQL Server uses its recursive CTE form and keeps `MAXRECURSION` in its engine builder.
- [ ] Model repeated predicates/order terms and nested/set queries as structured fragments. Empty collections and list expansion need explicit APIs and documented SQL; they must not silently create invalid `IN ()` text.
- [ ] Keep engine-only syntax and return/generated-value behavior in runtime engine builders. An engine builder may implement or extend `IQueryBuilder`. Reject child builders from an incompatible engine before execution.
- [ ] Do not expose `IQueryable`, expression trees, entity tracking, or SQL inferred from arbitrary C# lambdas.

Tests:

- [ ] Table-driven unit tests assert exact SQL, parameter order/name/value/type hints, and command type for every common operation across all engines.
- [ ] Safety tests cover quote characters, SQL comments, semicolons, malicious values, generated constants, user const strings, rejected non-const raw expressions, nulls, repeated arguments, escaped braces, and nested parameter-name collisions.
- [ ] Reuse tests build the same child into multiple parents and concurrently build independent commands without text or parameter mutation.
- [ ] Join tests prove the same generated relationship constant works with every supported join kind and that source aliases rewrite the predicate. Cross-join tests prove no `ON` clause is emitted.
- [ ] Dialect tests cover recursive CTE rendering, paging, identifier quoting, parameter markers, stored-procedure command type, and every declared engine deviation.

Done: `IQueryBuilder` trees compose valid parameterized CRUD, joins, CTEs, recursive CTEs, and correlated subqueries for all five engines; only compile-time constant `:raw` values enter command text.
