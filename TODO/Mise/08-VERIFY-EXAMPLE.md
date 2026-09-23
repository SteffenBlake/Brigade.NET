# 08 Verification and example

Depends: 01-07.

- [ ] Define one provider-neutral schema, seed set, mapped model set, and behavioral assertion suite. Reuse assertions across engine fixtures; isolate engine-specific setup and capability tests.
- [ ] Run disposable pinned SQL Server, PostgreSQL, MySQL, and MariaDB containers plus an isolated SQLite database in CI. Add health/readiness checks, per-test database isolation, bounded startup timeouts, and failure logs.
- [ ] Test const identifiers inside attributes, engine-owned schemas, aliases and alias-qualified relationship constants used in `FormattableString` joins, runtime values bound as join parameters, every supported join, CTE/recursive CTE, correlated and set queries, nested query parameter names, parameters/raw diagnostics, `IQueryBuilder` reads, `ICommandBuilder` CRUD and other writes, generated values, composite keys, all row shapes, transactions, `FirstOrNotFound`, cancellation, and `NULL` mismatch.
- [ ] Keep provider deviations in an executable capability matrix. A documented supported cell links to a passing test; an unsupported cell links to a diagnostic or capability-error test.
- [ ] Add benchmarks for incremental generation, ordinal binding/materialization, parameter construction, buffered reads, streaming reads, and writes. Store environment and baseline data. Report regressions in CI first; block only after a stable threshold and repeat policy are documented.
- [ ] Add a private Example AppHost helper that runs a pinned Liquibase image with changelog mount, JDBC URL, credentials, optional contexts/labels, dependency/wait wiring, captured logs, and non-zero exit failure.
- [ ] Keep Liquibase code under Example. Mise source and packages must not reference Aspire, Liquibase, JDBC, or migration types.
- [ ] Add example routes showing generated table/column metadata, two aliases of one table, a join, CTE, row mapping, safe custom SQL, stored procedure where the selected engine supports it, reader query, writer command, and `UnitOfWorkPartie` commit/rollback.
- [ ] Never place real credentials in source, snapshots, logs, or exceptions. Example secrets come from configuration and test containers use throwaway credentials.
- [ ] Pack every runtime/analyzer pair and consume it from clean temporary fixtures with only package references. Build with restore disabled after fixture restore to catch undeclared dependencies.

Tests:

- [ ] Unit and generator test projects pass before container tests.
- [ ] Each engine fixture runs the same shared test list and reports skipped capability cases with a documented reason; silent conditional returns are forbidden.
- [ ] Example integration tests start AppHost, wait for Liquibase completion and the web health endpoint, call read/write routes, and verify committed and rolled-back database state.
- [ ] Package fixtures compile and execute one generated read per engine.
- [ ] CI publishes test results, container/Liquibase logs on failure, coverage, and benchmark artifacts.

Done: a clean checkout restores, builds, runs all unit/generator/Partie/package tests, passes the shared suite on five databases, launches the migrated Example, and packs artifacts usable without project references.
