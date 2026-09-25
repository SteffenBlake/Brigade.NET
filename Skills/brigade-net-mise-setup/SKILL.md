---
name: brigade-net-mise-setup
description: Set up Brigade.Net.Mise engine packages, DB config, Partie routes, readers, writers, and transactions.
---

# Mise setup

Use `Brigade.Net.Mise` plus one engine pair:

| Engine | Runtime | Analyzer |
|---|---|---|
| SQL Server | `Brigade.Net.Mise.SqlServer` | `Brigade.Net.Mise.Engines.SqlServer` |
| PostgreSQL | `Brigade.Net.Mise.PostgreSQL` | `Brigade.Net.Mise.Engines.PostgreSQL` |
| SQLite | `Brigade.Net.Mise.SQLite` | `Brigade.Net.Mise.Engines.SQLite` |
| MySQL | `Brigade.Net.Mise.MySQL` | `Brigade.Net.Mise.Engines.MySQL` |
| MariaDB | `Brigade.Net.Mise.MariaDb` | `Brigade.Net.Mise.Engines.MariaDb` |

Also add the provider package/factory. Analyzer package generates table constants, join constants, and row mapping. One mapped type uses one engine.

Config contract: `IDbConfig.ConnectionString`, `IDbConfig.ProviderFactory`. Route config:

```csharp
new DbRouteConfig(connectionString, providerFactory)
```

Register keyed `DbProviderFactory` by connection-string name. Route setup:

```csharp
[UnitOfWorkPartie]
[DbReaderProvider]
[DbWriterTxnProvider]
[DbWriterProvider]
[DbConfigProvider("SqlServer")]
```

`DbConfigProvider` resolves `IConfiguration.GetConnectionString(name)` and keyed factory. Reader picks last `IDbConfig`; writer transaction is lazy; writer requires `DbWriterTxn`. `DbReader` owns config-created connection; supplied connection stays caller-owned. `DbWriterTxn` owns writer and transaction; outer UoW disposes transaction before writer. Use reader for queries; writer for commands. Use `[Provide] DbReader` / `[Provide] DbWriter` in handler context. Read `brigade-net-partie` for route/provider ordering and handler contracts.

Public runtime API:

```csharp
new DbReader(IDbConfig? config = null, DbConnection? connection = null)
ListAsync<T>(IQueryBuilder query, CancellationToken ct = default)
StreamAsync<T>(IQueryBuilder query, CancellationToken ct = default)
FirstOrNotFoundAsync<T>(IQueryBuilder query, CancellationToken ct = default)
ScalarAsync<T>(IQueryBuilder query, CancellationToken ct = default)
ExistsAsync(IQueryBuilder query, CancellationToken ct = default)

new DbWriter(IDbConfig? config = null, DbConnection? connection = null, DbTransaction? transaction = null)
Transaction
BeginTransactionAsync(CancellationToken ct = default)
ExecuteAsync(ICommandBuilder command, CancellationToken ct = default)
ExecuteScalarAsync<T>(ICommandBuilder command, CancellationToken ct = default)
ReturningListAsync<T>(ICommandBuilder command, CancellationToken ct = default)
ReturningFirstOrNotFoundAsync<T>(ICommandBuilder command, CancellationToken ct = default)
```

All row terminals require `T : IRow<T>`. Operations return `Result<...>`; provider/DB errors throw. Reader/writer allow one active operation; dispose async. Streams hold reader until exhausted/disposed. `DbWriter.Transaction` implements `ITxn`; use with `UnitOfWork`. Never dispose writer before txn adapter/UoW. `DbReader(config, connection)` needs exactly one source. Writer owns a supplied transaction; `DisposeAsync()` is the resource API.

Txn API: `CommitAsync(CancellationToken ct = default)`, `RollbackAsync(CancellationToken ct = default)`, `DisposeAsync()`. UoW: `new UnitOfWork(IEnumerable<ITxn>)`, `.AddTxn(Func<CancellationToken, Task>? commit = null, Func<CancellationToken, Task>? rollback = null)`, `.CommitAsync(ct)`, `.RollbackAsync(ct)`, `.DisposeAsync()`.
