using System.Data.Common;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Mise;

internal sealed class MiseTxnAdapter(DbWriter writer, DbTransaction? transaction = null) : ITxn
{
    private DbTransaction? _transaction = transaction;
    private bool _completed;
    private bool _disposed;

    internal DbTransaction? ProviderTransaction => _transaction;

    internal bool IsDisposed => _disposed;

    internal async ValueTask EnsureStartedAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
        {
            throw new InvalidOperationException("The Mise transaction has completed.");
        }

        _transaction ??= await connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        writer.BeginTransactionOperation();
        try
        {
            if (_completed)
            {
                return;
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_transaction is not null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }

            _completed = true;
        }
        finally
        {
            writer.EndTransactionOperation();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        writer.BeginTransactionOperation();
        try
        {
            if (_completed)
            {
                return;
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            try
            {
                if (_transaction is not null)
                {
                    await _transaction.RollbackAsync(cancellationToken);
                }
            }
            finally
            {
                _completed = true;
            }
        }
        finally
        {
            writer.EndTransactionOperation();
        }
    }

    public async ValueTask DisposeAsync()
    {
        writer.BeginTransactionOperation();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
            }
        }
        finally
        {
            writer.EndTransactionOperation();
        }
    }
}
