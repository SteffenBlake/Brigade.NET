# 07 Partie extension

Depends: 06.

- [x] Add generic providers in Partie.Extensions.Mise for `DbReader`, `DbWriter`, and the writer transaction as `ITxn` using normal Partie contracts. Provider contexts obtain `IMiseConfig` through `[Provide]`.
- [x] Use `IQueryProvider` for readers and `ICommandProvider` for writers. Generated registration attributes come from the existing Partie engine; Mise adds no custom route-registration mechanism.
- [x] Reader provider opens one connection only when a downstream `[Provide] DbReader` requests it. It owns and asynchronously disposes the reader/connection after `next` completes or throws.
- [x] Because Partie resolves provided values by exact type, expose `ITxn` and `DbWriter` as two coordinated provider outputs over one shared lazy transaction scope. The writer provider obtains that scope through Partie context resolution; it must not open a second connection or transaction.
- [x] The shared scope opens its connection and transaction only when `DbWriter` first needs them. Its `ITxn` commit/rollback is a no-op if the writer was never used. Providers never choose the outcome; existing `UnitOfWorkPartie` does.
- [x] Use the existing `[UnitOfWorkPartie]` on root. Its `UnitOfWorkContext([Provide] IEnumerable<ITxn>)` gathers the Mise transaction, commits success and deprecated results, rolls back failures, and rolls back/rethrows exceptions and cancellation.
- [x] Registration order must place `UnitOfWorkPartie` outside both Mise command providers while allowing its forward `[Provide] IEnumerable<ITxn>` lookup. Document the canonical route attribute order with a compile-tested example.
- [x] Query routes never register a writer transaction or `UnitOfWorkPartie`. A command that never requests `DbWriter` opens no connection or transaction.
- [x] Support multiple databases through route-specific config provider overrides. Each route selects its engine's config and distinct generated table identifiers while the handler follows the same logical query flow. Use distinct config wrapper types or closed generic provider types where exact-type Partie resolution needs them. Provide a compile-tested two-database route example.
- [x] Add a ServiceNames static class to ServiceDefaults class, with const string names for each database type and "WebApp", names must be no space, no underscore, PascalCase, just use `nameof` of itself so it is its own name
- [x] Create a config provider in Partie.Extensions.Mise that takes in the connection string name as a param, pass in ServiceNames.TheServiceName, it should inject standard aspnet config and pull the connection string from that by name, Aspire will provide these connection strings in phase 8

Current route order: place `[UnitOfWorkPartie]` on the root route group, then put `[MiseConfigProvider(ServiceNames.Sqlite)]`, `[MiseTransactionProvider]`, `[MiseWriterProvider]`, and the command route attribute on each write route, in that order. Query routes use the config provider, then `[MiseReaderProvider]`, then the query route attribute. Register a keyed `DbProviderFactory` under each matching `ServiceNames` name. The config provider reads `ConnectionStrings:{name}` from `IConfiguration` and resolves that name's keyed factory. Each route can thus select a different connection and engine. The generated route compilation tests cover two names and both pipeline shapes; phase 8 will supply the Aspire registrations and generated engine table identifiers.

Tests:

- [x] Direct provider tests cover lazy creation, one invocation, async disposal, downstream `Result` failure, exception, cancellation, and an unused writer.
- [x] Partie route/generator tests prove generated registration attributes resolve `DbReader`, `DbWriter`, `ITxn`, and two distinct configs in source order.
- [x] End-to-end command tests use the real `UnitOfWorkPartie` and assert exactly one commit for success/deprecated, exactly one rollback for every failure case and downstream throw/cancel, and rollback after commit failure.
- [x] Query tests assert no transaction starts. Command short-circuit tests assert no provider resource opens unless a downstream context requests it.

Done: generated route tests prove lazy reader/writer/config resolution, and existing `UnitOfWorkPartie` tests with the Mise `ITxn` adapter prove exactly one transaction outcome at each route scope.
