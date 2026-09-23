# Mise decision log

Keep entries short: `YYYY-MM-DD | phase | decision | reason | affected tasks`.

Only record choices that change contracts, dependencies, scope, or completion tests. Do not log routine implementation detail.

- 2026-09-23 | all | Five engines are SQL Server, PostgreSQL, SQLite, MySQL, MariaDB | duplicate SQL Server entry meant SQLite | 01,04,05,08
- 2026-09-23 | execution | Public DB operations use `Result<T>`; DB NULL to non-nullable throws | required behavior | 02,06
- 2026-09-23 | query | Interpolation parameterizes by default; `:raw` requires a compile-time const string enforced by analyzer | preserve explicit SQL composition without allowing runtime values into command text | 03,05
- 2026-09-23 | scope | No LINQ means Mise exposes its own SQL-shaped fluent API and no IQueryable/expression translation; internal LINQ is allowed | fluent API mirrors SQL operations such as With | all
- 2026-09-23 | contracts | Engine-specific schema behavior and attributes live in engine packages | core cannot impose SQL Server dbo semantics on other engines | 02,04
- 2026-09-23 | generation | Phase 1 Tbl.cs table, column, alias, and relationship members are const strings | generated values must work in attributes and raw interpolation | 03,04,05
- 2026-09-23 | joins | Relationship constants contain target plus ON predicate; fluent method supplies join kind; aliases generate requalified relationships | one generated relationship works for inner/outer join forms | 02,04,05
- 2026-09-23 | query | DbReader and DbWriter accept IQueryBuilder trees; engine packages may add specialized builders | nested, correlated, CTE, recursive, and dialect-specific queries share one execution boundary | 05,06
- 2026-09-23 | execution | Expose FirstOrNotFound but no First, Single, or OrDefault family | avoid LINQ-shaped terminal API | 06
- 2026-09-23 | execution | ADO.NET and provider exceptions flow out unchanged | do not replace provider error contracts | 02,06
- 2026-09-23 | generation | Attributed partial result types receive a static-abstract ADO.NET materialization interface implementation | compile-time mapping without reflection or per-row name lookup | 02,04,06
- 2026-09-23 | partie | DbWriter exposes an ITxn adapter to existing UnitOfWorkPartie; no second transaction Partie | reuse Partie transaction ownership | 06,07
- 2026-09-23 | engines | MySQL and MariaDB remain separate engines but may share internal helpers | their syntax already differs and separate packages allow future divergence | 01,04,05,08
- 2026-09-23 | tests | Use focused generator assertions and compile/run tests; reserve exact output for escaping, names, ordering, and packaging | catch contract changes without formatting-only churn | 03,04,08
- 2026-09-23 | contracts | Row materialization binds projected names once to an ordinal array, then uses one exact constructor plus an object initializer through `IMiseRow<T>` | deterministic support for records, structs, required/init members, and reflection-free per-row reads | 02,03,04,06
- 2026-09-23 | contracts | Constructor parameters match mapped property names with ordinal case-insensitive comparison and exact CLR types | supports normal camel-case parameters without reflection heuristics | 02,03,04
- 2026-09-23 | contracts | SQL Server and PostgreSQL own schema attributes; MySQL and MariaDB own database attributes; SQLite has neither | qualification semantics differ by engine and must not leak into core | 02,04
- 2026-09-23 | dependencies | Mise core references Brigade.Net.Core for public `Result<T>` terminals | database operations must compose directly with handlers | 01,02,06
- 2026-09-23 | generation | Emit formatted C# in one generated file per mapped source target | generated code must stay readable and traceable to its source instead of becoming a monolithic output | 03,04,08
- 2026-09-23 | contracts | Each engine owns a distinctly named table attribute derived from `TableAttributeBase`; one type cannot select two engines | multiple engine packages must coexist without every generator reacting to the same table marker or emitting conflicting output | 02,03,04,08
- 2026-09-23 | contracts | Each engine also owns a distinctly named row attribute derived from `RowAttributeBase`; table and row markers on one type must select the same engine | a shared row marker would make every installed engine generator emit duplicate materializers | 02,03,04,08
- 2026-09-23 | generation | MISE016 rejects generated Tbl member name collisions | invalid generated C# must be reported at the source target | 04
- 2026-09-23 | generation | Unqualified table constants leave schema or database selection to the connection; explicit engine attributes add qualification | preserve configured SQL Server default schema, PostgreSQL search path, and selected MySQL/MariaDB database | 04
- 2026-09-23 | generation | Generated ordinal binding matches exact projected names first, then case-insensitive names for SQL Server, SQLite, MySQL, and MariaDB; PostgreSQL requires exact case | make alias binding stable across ADO.NET provider lookup rules | 04
- 2026-09-23 | scope | Move join capability checks, unsupported-join diagnostics, and their no-SQL test from 04 to 05 | they require fluent join operations; phase 04 generator work and tests are complete | 04,05
