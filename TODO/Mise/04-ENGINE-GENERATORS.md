# 04 Engine generators

Depends: 03.

- [ ] Implement one incremental generator per engine: SQL Server, PostgreSQL, SQLite, MySQL, and MariaDB. Each generator uses shared parsing and owns dialect quoting and capability checks.
- [ ] Every phase 1 member generated onto a partial `Tbl.cs` type is a `const string`, including table identifiers, column identifiers, aliases, and relationship join fragments. Do not emit phase 1 properties, objects, or `static readonly` fields. Each value must work as an attribute argument and a `:raw` input.
- [ ] Apply engine-owned schema attributes and defaults while creating phase 1 constants. Core generator models must not assume `dbo`, `public`, or another engine default.
- [ ] Generate one nested constant container per declared alias. Its table and column constants use the alias. Its relationship constants use the alias on the source side. Multiple aliases of one table must coexist.
- [ ] Generate each relationship constant as the syntax after the fluent method's join keyword: quoted target source, `ON`, and the quoted predicate. Do not include `INNER`, `LEFT`, `RIGHT`, or `FULL`; the same constant must work with every supported join method.
- [ ] Diagnose use of a join kind unsupported by the selected engine. Cross joins use table/alias constants or child builders and do not use relationship constants.
- [ ] Phase 2 row generation adds the static-abstract materialization interface to each attributed partial result type. Generate one ordinal-binding method and one typed row-reading method. Do not use reflection, `Activator`, or per-row property-name lookup.
- [ ] Define case sensitivity for ordinal names per engine/provider and test aliased projections. Column aliases used for row mapping must be deterministic.
- [ ] Generate engine-specific metadata only from attributes in the paired runtime engine package. Core source must compile without all five runtime packages installed. Keep MySQL and MariaDB as separate engines even when their implementations share helper code.
- [ ] Keep implementation helpers private and generated consumer-facing members internal unless the mapped user type is public and the API must be named by calling code.

Tests:

- [ ] Focused generated-member and compile/run tests cover all five dialects, every identifier quote escape, engine-owned schemas, reserved words, Unicode, aliases, relationship constants, and symbol collisions.
- [ ] Exact-output tests cover const qualification, escaping, hint names, deterministic ordering, and analyzer package contents. Other generator tests do not fail for formatting-only changes.
- [ ] Fake-reader tests prove ordinal lookup occurs once, typed reads use correct ordinals, nullable values map, and non-nullable `NULL` throws the required exception.
- [ ] Dialect capability tests assert the diagnostic for unsupported joins and assert no SQL is emitted for the rejected operation.
- [ ] A consumer compile test installs only one runtime/analyzer pair and proves no other engine assembly is required.

Done: the same sample produces compiling phase 1 const strings and phase 2 static materializers for all five engines; aliases rewrite relationship qualification and generated constants compile inside attributes.
