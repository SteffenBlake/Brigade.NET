# 07 Partie extension

Depends: 06.

- [ ] Add lazy generic providers for `DbReader`, `DbWriter`, and the writer transaction as `ITxn` using normal Partie contracts. Provider contexts obtain `IMiseConfig` through `[Inject]`.
- [ ] Use `IQueryProvider` for readers and `ICommandProvider` for writers. Generated registration attributes come from the existing Partie engine; Mise adds no custom route-registration mechanism.
- [ ] Reader provider opens one connection only when a downstream `[Provide] DbReader` requests it. It owns and asynchronously disposes the reader/connection after `next` completes or throws.
- [ ] Because Partie resolves provided values by exact type, expose `ITxn` and `DbWriter` as two coordinated provider outputs over one shared lazy transaction scope. The writer provider obtains that scope through Partie context resolution; it must not open a second connection or transaction.
- [ ] The shared scope opens its connection and transaction only when `DbWriter` first needs them. Its `ITxn` commit/rollback is a no-op if the writer was never used. Providers never choose the outcome; existing `UnitOfWorkPartie` does.
- [ ] Use the existing `[UnitOfWorkPartie]` on write routes. Its `UnitOfWorkContext([Provide] IEnumerable<ITxn>)` gathers the Mise transaction, commits success and deprecated results, rolls back failures, and rolls back/rethrows exceptions and cancellation.
- [ ] Registration order must place `UnitOfWorkPartie` outside both Mise command providers while allowing its forward `[Provide] IEnumerable<ITxn>` lookup. Document the canonical route attribute order with a compile-tested example.
- [ ] Query routes never register a writer transaction or `UnitOfWorkPartie`. A command that never requests `DbWriter` opens no connection or transaction.
- [ ] Support multiple databases through distinct config wrapper types or closed generic provider types, since Partie resolution uses exact types. Provide a compile-tested two-database route example; do not rely on ambiguous repeated `IMiseConfig` values.

Tests:

- [ ] Direct provider tests cover lazy creation, one invocation, async disposal, downstream `Result` failure, exception, cancellation, and an unused writer.
- [ ] Partie route/generator tests prove generated registration attributes resolve `DbReader`, `DbWriter`, `ITxn`, and two distinct configs in source order.
- [ ] End-to-end command tests use the real `UnitOfWorkPartie` and assert exactly one commit for success/deprecated, exactly one rollback for every failure case and downstream throw/cancel, and rollback after commit failure.
- [ ] Query tests assert no transaction starts. Command short-circuit tests assert no provider resource opens unless a downstream context requests it.

Done: generated route tests prove lazy reader/writer/config resolution, and existing `UnitOfWorkPartie` tests with the Mise `ITxn` adapter prove exactly one transaction outcome at each route scope.
