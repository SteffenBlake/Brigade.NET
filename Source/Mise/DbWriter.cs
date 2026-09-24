using System.Data;
using System.Data.Common;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Core.Results;

namespace Brigade.Net.Mise;

/// <summary>Executes write commands against one non-concurrent connection scope.</summary>
/// <remarks>
/// Configuration creates an owned connection. An existing connection remains caller-owned.
/// Commands are always asynchronously disposed, and provider exceptions escape unchanged.
/// The returned transaction adapter owns the provider transaction; a UnitOfWork should dispose
/// that adapter before this writer is disposed. The writer owns no caller-supplied connection.
/// </remarks>
/// <param name="config">Configuration for an owned connection.</param>
/// <param name="connection">An existing caller-owned connection.</param>
/// <param name="transaction">An existing transaction whose ownership transfers to the returned adapter.</param>
public class DbWriter(
    IDbConfig? config = null,
    DbConnection? connection = null,
    DbTransaction? transaction = null
) : DbReader(config, ResolveConnection(config, connection, transaction))
{
    private DbTxnAdapter? _adapter;

    internal void BeginTransactionOperation() => Enter();

    internal void EndTransactionOperation() => Exit();

    /// <summary>Gets one lazy transaction adapter for this writer. A UnitOfWork owns and disposes it.</summary>
    public ITxn Transaction => _adapter ??= new DbTxnAdapter(this, transaction);

    /// <summary>Disposes the writer after its transaction adapter has been disposed by its owner.</summary>
    public override async ValueTask DisposeAsync()
    {
        if (_adapter is null && transaction is not null)
        {
            _adapter = new DbTxnAdapter(this, transaction);
            await _adapter.DisposeAsync();
        }

        if (_adapter is { IsDisposed: false })
        {
            throw new InvalidOperationException("Dispose the writer transaction before the writer.");
        }

        await base.DisposeAsync();
    }

    /// <summary>Begins the provider transaction now and returns the same adapter.</summary>
    public async Task<ITxn> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        Enter();
        try
        {
            var adapter = _adapter ??= new DbTxnAdapter(this, transaction);
            var activeConnection = await GetConnectionAsync(cancellationToken);
            await adapter.EnsureStartedAsync(activeConnection, cancellationToken);
            return adapter;
        }
        finally
        {
            Exit();
        }
    }

    /// <summary>Executes a write and returns its affected-row count, including zero.</summary>
    public async Task<Result<int>> ExecuteAsync(
        ICommandBuilder query,
        CancellationToken cancellationToken = default
    )
    {
        Enter();
        try
        {
            await using var command = await CreateCommandAsync(query.Compile(), cancellationToken);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            Exit();
        }
    }

    /// <summary>Executes a write command and returns its scalar value, with database NULL as null.</summary>
    public async Task<Result<T?>> ExecuteScalarAsync<T>(
        ICommandBuilder commandBuilder,
        CancellationToken cancellationToken = default
    )
    {
        Enter();
        try
        {
            await using var command = await CreateCommandAsync(commandBuilder.Compile(), cancellationToken);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value is null or DBNull ? default : (T)value;
        }
        finally
        {
            Exit();
        }
    }

    /// <summary>Executes a returning command and maps all returned rows.</summary>
    public async Task<Result<IReadOnlyList<T>>> ReturningListAsync<T>(
        ICommandBuilder commandBuilder,
        CancellationToken cancellationToken = default
    )
        where T : IRow<T>
    {
        Enter();
        try
        {
            var compiled = commandBuilder.Compile();
            await using var command = await CreateCommandAsync(compiled, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(compiled.Behavior, cancellationToken);
            var ordinals = T.BindOrdinals(reader);
            var rows = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(T.Materialize(reader, ordinals));
            }
            return rows;
        }
        finally
        {
            Exit();
        }
    }

    /// <summary>Executes a returning command and maps its first row, or returns NotFound.</summary>
    public async Task<Result<T>> ReturningFirstOrNotFoundAsync<T>(
        ICommandBuilder commandBuilder,
        CancellationToken cancellationToken = default
    )
        where T : IRow<T>
    {
        Enter();
        try
        {
            var compiled = commandBuilder.Compile();
            await using var command = await CreateCommandAsync(compiled, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(
                compiled.Behavior | CommandBehavior.SingleRow,
                cancellationToken
            );
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new NotFound();
            }

            var ordinals = T.BindOrdinals(reader);
            return T.Materialize(reader, ordinals);
        }
        finally
        {
            Exit();
        }
    }

    /// <inheritdoc />
    protected override async ValueTask BeforeCreateCommandAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    )
    {
        if (_adapter is not null || transaction is not null)
        {
            await ((_adapter ??= new DbTxnAdapter(this, transaction))
                .EnsureStartedAsync(connection, cancellationToken));
        }
    }

    /// <inheritdoc />
    protected override void ConfigureCommand(DbCommand command)
    {
        if (_adapter?.ProviderTransaction is { } providerTransaction)
        {
            command.Transaction = providerTransaction;
        }
    }

    private static DbConnection? ResolveConnection(
        IDbConfig? config,
        DbConnection? connection,
        DbTransaction? transaction
    )
    {
        if (transaction is null)
        {
            return connection;
        }

        var transactionConnection = transaction.Connection
            ?? throw new ArgumentException("The transaction has no connection.", nameof(transaction));
        if (config is not null || connection is not null && !ReferenceEquals(connection, transactionConnection))
        {
            throw new ArgumentException("The transaction must use the writer's connection.", nameof(transaction));
        }

        return transactionConnection;
    }
}
