# Mise work index

Agent rule: read this file, then the active numbered file and its dependencies. Check an item only after the stated tests pass. Record contract, dependency, or scope changes in `09-DECISIONS.md`. Do not broaden scope.

## Architecture baseline

Mise follows the Expo split already used by this repository:

- .NET 10 runtime assemblies hold public attributes, contracts, builders, and execution code.
- `netstandard2.0` Roslyn assemblies hold incremental generators. Engine analyzers reuse a generator-helper assembly and package that dependency with the analyzer, as `Partie.Engines.AspNetCore` does for `Partie.Generator`.
- Generator tests run `CSharpGeneratorDriver`, assert diagnostics and generated text, and compile the generated output. Runtime behavior stays in separate .NET 10 xUnit projects.
- Generators emit readable, consistently formatted C# with four-space indentation and LF line endings. Each mapped source target gets its own generated file; generators must not combine unrelated targets into one monolithic output.
- Public runtime APIs have XML documentation and treat missing documentation as an error, matching existing public projects.

## Fixed requirements

- Target .NET 10 for runtime code and `netstandard2.0` for Roslyn components. Use ADO.NET provider APIs only.
- “No LINQ” means no `IQueryable`, expression-tree translation, or LINQ-style database provider. Internal in-memory LINQ in runtime, tests, or generators is allowed.
- Public async database operations return `Task<Result<T>>` or `ValueTask<Result<T>>`. ADO.NET and provider exceptions flow out unchanged. `FirstOrNotFound` returns `NotFound` when no row exists; Mise exposes no LINQ-style `First`, `Single`, or `OrDefault` family.
- A database `NULL` mapped to a non-nullable C# member throws `MiseMappingException` with result type, member, column name, and ordinal.
- Every `FormattableString` hole becomes a parameter unless its format is exactly `raw`. A Roslyn analyzer permits `:raw` only for compile-time constant strings, including generated Mise constants.
- Generated/user API stays internal or private unless a consumer must name it. Public APIs have XML docs.
- Keep one generated file per mapped source target with a stable, collision-free hint name. Generated C# must be formatted for human review.
- Support SQL Server, PostgreSQL, SQLite, MySQL, and MariaDB. Integration tests run against each provider. MySQL and MariaDB share code only where their observable behavior is the same.
- Core stays engine-neutral. Engine-specific schema, syntax, options, and attributes live in the matching .NET 10 engine package. Matching analyzer packages interpret those attributes without adding runtime-to-generator references.
- Each engine runtime owns a distinctly named table attribute: `SqlServerTable`, `PostgreSqlTable`, `SqliteTable`, `MySqlTable`, or `MariaDbTable`. They derive from the abstract core `TableAttributeBase`. A mapped type may use only one engine table attribute; multiple engine table attributes are a compile-time error.
- Row targets use matching engine-owned markers (`SqlServerRow`, `PostgreSqlRow`, `SqliteRow`, `MySqlRow`, or `MariaDbRow`) derived from core `RowAttributeBase`. A type cannot mix row markers or use table and row markers from different engines.
- `DbReader` and `DbWriter` execute `IQueryBuilder`. Engine packages may provide specialized builders while preserving the shared execution contract.
- The Example owns Liquibase and Aspire wiring. Mise packages expose no migration API.
- Existing `UnitOfWorkPartie` owns transaction completion. Mise supplies an `ITxn`; it does not add a second commit/rollback Partie.

## Order

- [x] [01 Projects](01-PROJECTS.md)
- [x] [02 Contracts](02-CONTRACTS.md)
- [x] [03 Generator core](03-GENERATOR-CORE.md)
- [ ] [04 Engine generators](04-ENGINE-GENERATORS.md)
- [ ] [05 Query API](05-QUERY-API.md)
- [ ] [06 Execution](06-EXECUTION.md)
- [ ] [07 Partie](07-PARTIE.md)
- [ ] [08 Verification and example](08-VERIFY-EXAMPLE.md)

## Required test layers

- Contract tests: attribute shape, API visibility, XML docs, and engine-neutral dependency boundaries.
- Generator tests: exact diagnostic ID, severity, location, and message; focused generated-member assertions; generated compilation and execution; exact output only for escaping, hint names, ordering, and package stability.
- Runtime unit tests: SQL composition, parameter binding, mapping, row selection, ownership, disposal, cancellation, and exception behavior.
- Partie tests: lazy provider resolution and existing `UnitOfWorkPartie` outcomes, including deprecated results and commit failure.
- Integration tests: the same behavioral suite against all five engines, with engine-specific cases separated and named.
- Package tests: pack each runtime/analyzer pair, consume it from a clean fixture, and prove the analyzer and its helper dependencies load without direct helper references.

## Global completion

- [ ] `dotnet restore`, `dotnet build`, and all xUnit projects pass from a clean checkout.
- [ ] Generator diagnostics, generated-compilation tests, runtime unit tests, Partie tests, package-fixture tests, and five-engine integration tests pass.
- [ ] Coverage includes every public terminal operation and every failure branch named in these requirements. Generated source text itself is excluded; generator behavior is covered through input/output tests.
- [ ] The Example proves schema migration, generated identifiers and mapping, parameterized CRUD/query, aliases, joins, CTEs, and Partie commit/rollback.
- [ ] Public API docs state ownership, disposal, null/row-selection/error behavior, parameter safety, concurrency limits, and engine deviations.

## Non-goals

`IQueryable` or expression translation; runtime reflection mapping; change tracking; lazy loading; migrations; masking unsupported dialect features behind false portability; public Liquibase/Aspire packages.
