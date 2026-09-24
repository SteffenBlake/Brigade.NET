# 08 Verification and example

Depends: 01-07.

- [ ] Define one provider-neutral schema, seed set, mapped model set, and behavioral assertion suite. Reuse assertions across engine fixtures; isolate engine-specific setup and capability tests.
- [x] Give each engine its own mapped model set and generated `Tbl.cs` identifiers. Keep the logical tables, columns, relationships, and seed rows aligned, including reserved-word column names, nullable columns, composite keys, and values that test parameter safety. Record any schema difference required by a provider.
- [ ] Run disposable pinned SQL Server, PostgreSQL, MySQL, and MariaDB containers plus an isolated SQLite database in CI. Add health/readiness checks, per-test database isolation, bounded startup timeouts, and failure logs.
- [ ] Test const identifiers inside attributes, engine-owned schemas, aliases and alias-qualified relationship constants used in `FormattableString` joins, runtime values bound as join parameters, every supported join, CTE/recursive CTE, correlated and set queries, nested query parameter names, parameters/raw diagnostics, `IQueryBuilder` reads, `ICommandBuilder` CRUD and other writes, generated values, composite keys, all row shapes, transactions, `FirstOrNotFound`, cancellation, and `NULL` mismatch.
- [ ] Keep provider deviations in an executable capability matrix. A documented supported cell links to a passing test; an unsupported cell links to a diagnostic or capability-error test.
- [ ] Add benchmarks for incremental generation, ordinal binding/materialization, parameter construction, buffered reads, streaming reads, and writes. Store environment and baseline data. Report regressions in CI first; block only after a stable threshold and repeat policy are documented.
- [x] Add a private Example AppHost helper that runs a pinned Liquibase image with changelog mount, JDBC URL, credentials, optional contexts/labels, dependency/wait wiring, captured logs, and non-zero exit failure.
- [x] Have the Example AppHost start all five databases, using an isolated SQLite file, and run one Liquibase changelog per engine before the web app starts. Keep the five schemas as close as each engine allows. The web app must receive each connection setting from Aspire resources.
- [x] Keep Liquibase code under Example. Mise source and packages must not reference Aspire, Liquibase, JDBC, or migration types.
- [ ] Add example routes showing generated table/column metadata, two aliases of one table, a join, CTE, row mapping, safe custom SQL, stored procedure where the selected engine supports it, reader query, writer command, and `UnitOfWorkPartie` commit/rollback.
- [x] Add one route for each database engine. Each route registers a config provider that selects that engine's Aspire connection settings and overrides the common config source for that route. Its handler uses that engine's distinct generated `Tbl.cs` identifiers and row types. Compile-test exact Partie provider resolution so one route cannot receive another engine's config.
- [ ] Run the same demanding logical read on all five routes, expressed with each engine's builders and generated identifiers. Include reserved-word identifiers, two aliases of one table, generated relationship joins, a runtime join parameter, nested and recursive CTEs, a correlated subquery, a set operation, grouping, ordering, paging, nullable values, escaped quotes, repeated interpolation holes, and enough nested parameters to test allocation order. Gate only syntax a provider truly lacks, and assert equivalent ordered results from the shared seed data.
- [x] Never place real credentials in source, snapshots, logs, or exceptions. Example secrets come from configuration and test containers use throwaway credentials.
- [ ] Pack every runtime/analyzer pair and consume it from clean temporary fixtures with only package references. Build with restore disabled after fixture restore to catch undeclared dependencies.

Tests:

- [ ] Unit and generator test projects pass before container tests.
- [ ] Each engine fixture runs the same shared test list and reports skipped capability cases with a documented reason; silent conditional returns are forbidden.
- [ ] Example integration tests start AppHost, wait for Liquibase completion and the web health endpoint, call read/write routes, and verify committed and rolled-back database state.
- [ ] Example integration tests call all five engine routes and compare the complex query's rows, column mapping, parameter order, and transaction results. They also prove each route used its own database by changing one engine's data without changing the other four.
- [ ] Package fixtures compile and execute one generated read per engine.
- [ ] CI publishes test results, container/Liquibase logs on failure, coverage, and benchmark artifacts.

Done: a clean checkout restores, builds, runs all unit/generator/Partie/package tests, passes the shared suite on five databases, launches the migrated Example, and packs artifacts usable without project references.
