using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Owns one lazy writer and its transaction for a command pipeline.</summary>
public sealed class DbWriterTxn(IDbConfig? config) : ITxn
{
    private DbWriter? _writer;
    private bool _disposed;

    /// <summary>Gets the writer without opening its connection or starting its transaction.</summary>
    public DbWriter Writer
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (config is null)
            {
                throw new InvalidOperationException("A database configuration is required for DbWriter.");
            }
            var writer = _writer ??= new DbWriter(config: config);
            _ = writer.Transaction;
            return writer;
        }
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _writer?.Transaction.CommitAsync(cancellationToken) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _writer?.Transaction.RollbackAsync(cancellationToken) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_writer is not null)
        {
            try
            {
                await _writer.Transaction.DisposeAsync();
            }
            finally
            {
                await _writer.DisposeAsync();
            }
        }
    }
}
