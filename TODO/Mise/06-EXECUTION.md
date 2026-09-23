# 06 Execution

Depends: 02, 05.

- [ ] Implement concrete `DbReader` and `DbWriter : DbReader` classes over `DbConnection`, `DbCommand`, `DbDataReader`, and `DbTransaction`.
- [ ] Each terminal takes an `IQueryBuilder`; execution builds the engine-specific command immediately before creating the ADO.NET command.
- [ ] Provide async terminals for list, `FirstOrNotFound`, scalar, stream, and exists where the shape is valid. Do not expose `First`, `Single`, `FirstOrDefault`, or `SingleOrDefault`. Use `IAsyncEnumerable<T>` for streaming and `[EnumeratorCancellation]` for its token.
- [ ] `FirstOrNotFound` reads at most the first row and returns `NotFound` when no row exists. It does not impose single-row cardinality.
- [ ] Writer terminals execute non-query, INSERT/UPDATE/DELETE, generated-value/returning, stored procedure, and trusted custom commands. Return affected-row counts unless a more specific method name documents another shape.
- [ ] Every public terminal accepts `CancellationToken`. Pass it to open, execute, read, commit/rollback adapter, and async disposal calls where the provider API permits.
- [ ] Create a fresh `DbCommand` and provider parameters from each immutable `MiseCommand`. Dispose commands/readers on success, `Result` failure, exception, and cancellation. A streaming enumerator owns them until enumeration completes or is disposed.
- [ ] Typed terminals constrain the result to the static-abstract generated materialization interface. Bind ordinals once per result set, then call the generated static reader for each row. Dynamic/reflection fallback is forbidden.
- [ ] Let all ADO.NET and provider exceptions escape unchanged, including cancellation, connection, command, reader, transaction, and disposal faults. Mise throws its own exception only for a fault it detects, such as invalid generated mapping or non-nullable `NULL`.
- [ ] Support command timeout and engine-neutral command behavior through immutable execution options. Engine-specific knobs stay in engine packages.
- [ ] `DbWriter` can begin or wrap one provider transaction and exposes one `ITxn` adapter for the existing `UnitOfWorkPartie`. The adapter completes and disposes the provider transaction exactly once.
- [ ] Instances and their connection/transaction are single-operation-at-a-time. Detect overlapping use and throw a documented `InvalidOperationException`; callers create separate readers for parallel work.

Tests:

- [ ] Fake-provider unit tests cover every terminal shape, `FirstOrNotFound` zero/one/many behavior, parameter transfer, cancellation propagation, overlap rejection, and exact disposal order.
- [ ] Streaming tests cover full enumeration, early break, consumer exception, cancellation, and never-enumerated streams.
- [ ] Mapping tests cover missing/duplicate ordinals, conversion failure, nullable `NULL`, and non-nullable `NULL` exception data.
- [ ] Transaction-adapter tests cover commit, rollback, double completion, commit failure, rollback failure, and disposal exactly once.
- [ ] The shared integration suite runs all terminal and transaction cases against five providers, with capability-gated generated-value cases.

Done: unit and five-engine integration tests pass every `IQueryBuilder` terminal, ownership, cancellation, mapping, concurrency, and transaction outcome named above.
