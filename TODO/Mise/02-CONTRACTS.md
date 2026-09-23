# 02 Contracts

Depends: 01.

## Table metadata

- [x] Define abstract `TableAttributeBase` in core and a distinctly named derived table attribute in each engine runtime package: `SqlServerTable`, `PostgreSqlTable`, `SqliteTable`, `MySqlTable`, and `MariaDbTable`. Each requires the table name. Reject null, empty, or whitespace identifiers and more than one engine table attribute on the same type at compile time.
- [x] Require `[MiseColumn(string name)]` on each mapped scalar property. Ignore static properties and indexers. Diagnose duplicate column names using the engine identifier comparer.
- [x] Define separate, single-purpose metadata for primary-key order, database-generated values, computed/read-only values, insert exclusion, and update exclusion. Validate contradictory combinations and require unique non-negative composite-key positions.
- [x] Use Roslyn nullability and `required` metadata for materialization rules. Attributes must not restate C# nullability.
- [x] Support repeatable aliases on tables. Alias names are unique per table after engine comparison. Each alias generates the same table, column, and relationship constants with alias qualification on the source side.
- [x] Define relationship metadata that identifies the source and target mapped columns. One relationship generates a reusable join constant containing the target source and `ON` predicate, but no join kind. The fluent API chooses inner, left, right, or full join. Cross join takes a table, alias, or child query and has no relationship predicate.

## Row targets

- [x] Define abstract `RowAttributeBase` in core and matching engine-owned row markers: `SqlServerRow`, `PostgreSqlRow`, `SqliteRow`, `MySqlRow`, and `MariaDbRow`. Class, struct, record class, and record struct targets opt in through one of these markers. Each target and containing type must be partial so generated code can add the required interface and static members.
- [x] Define an engine-neutral, static-abstract row materialization contract. Generated code binds projected column names to ordinals once per result set, then creates each target from `DbDataReader` and those ordinals.
- [x] Define and document one deterministic constructor/member selection contract before implementation. Diagnose zero or multiple valid materialization paths; never pick an ambiguous constructor or member by reflection-style heuristics.
- [x] Include inherited accessible mapped members in deterministic base-to-derived declaration order. Reject static, indexer, init-only-after-construction, inaccessible, duplicate, and unsupported ref-like members when the selected strategy cannot assign them.
- [x] Generated code may access private members only when emitted into the same partial type and C# permits access. It must not widen user member visibility.
- [x] A database `NULL` for nullable reference/value members maps to `null`. A `NULL` for a non-nullable member throws `MiseMappingException` with result type, member, column, and ordinal.

## Configuration and failures

- [x] Define engine-neutral `IMiseConfig` with `string ConnectionString` and `DbProviderFactory ProviderFactory`. Provider-specific options live in engine packages and produce this contract.
- [x] A reader/writer created from config owns the connection it opens. A reader/writer created from an existing connection does not own it. Commands and readers are always owned and asynchronously disposed by the terminal operation. Document both constructor paths.
- [x] Define Mise exceptions only for faults Mise detects itself, including invalid generated mapping and non-nullable `NULL`. Include safe structural context; never place parameter values or credentials in exception messages.
- [x] Public database operations return `Result<T>` so they compose with handlers. `FirstOrNotFound` returns `NotFound` for zero rows. Affected-row counts and zero-row writes remain successful data unless a named operation documents another contract.
- [x] Let cancellation, ADO.NET, provider, connection, command, reader, transaction, and disposal exceptions escape unchanged. Do not wrap, translate, or convert them to a `Result<T>` failure.

Tests:

- [x] Compile-time contract tests cover valid and invalid attribute combinations, partial/nested/generic shapes, accessibility, inheritance, constructor ambiguity, duplicate identifiers, and nullability.
- [x] Reflection-based tests may inspect the public contract in the test assembly only; Mise runtime code must contain no reflection mapper.
- [x] Ownership tests use fake `DbProviderFactory`, connection, command, and reader types to assert open/close/dispose counts and redacted exceptions.

Done: contract tests define every accepted model shape, public API review finds no dialect type in core, and a source scan/runtime test finds no reflection-based mapping path.
