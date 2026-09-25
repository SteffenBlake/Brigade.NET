namespace Brigade.Net.Core.Transactions;

/// <summary>
/// Coordinates commit/rollback across a group of <see cref="ITxn" /> instances.
/// Must be committed or rolled back before disposal.
/// </summary>
/// <param name="txns">The transactions to coordinate.</param>
public sealed class UnitOfWork(IEnumerable<ITxn> txns) : IAsyncDisposable
{
    private readonly List<ITxn> _txns = txns.ToList();
    private bool _isFinished;
    private bool _disposed;

    /// <summary>
    /// Adds a new <see cref="BasicTxn" /> built from the given delegates.
    /// </summary>
    /// <param name="commit">Invoked on commit, if provided.</param>
    /// <param name="rollback">Invoked on rollback, if provided.</param>
    /// <returns>This <see cref="UnitOfWork" />, for chaining.</returns>
    public UnitOfWork AddTxn(
        Func<CancellationToken, Task>? commit = null,
        Func<CancellationToken, Task>? rollback = null
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isFinished)
        {
            throw new InvalidOperationException("UnitOfWork has already completed.");
        }

        _txns.Add(new BasicTxn(commit, rollback));

        return this;
    }

    /// <summary>
    /// Commits every transaction. If any commit throws, rolls back every transaction
    /// and rethrows the original exception.
    /// </summary>
    /// <param name="cancellationToken">The route or caller cancellation token.</param>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isFinished)
        {
            throw new InvalidOperationException("UnitOfWork has already completed.");
        }

        try
        {
            foreach (var txn in _txns)
            {
                await txn.CommitAsync(cancellationToken);
            }

            _isFinished = true;
        }
        catch (Exception primary)
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await RollbackAsync(cleanup.Token);
            }
            catch (Exception rollbackFault)
            {
                primary.Data["UnitOfWork.RollbackException"] = rollbackFault;
            }

            throw;
        }
    }

    /// <summary>
    /// Rolls back every transaction. Each rollback runs in its own try/catch so a
    /// failure in one doesn't stop the rest; any failures are thrown together as an
    /// <see cref="AggregateException" />.
    /// </summary>
    /// <param name="cancellationToken">The route or caller cancellation token.</param>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isFinished)
        {
            return;
        }

        List<Exception>? exceptions = null;

        foreach (var txn in _txns)
        {
            try
            {
                await txn.RollbackAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        _isFinished = true;

        if (exceptions is not null)
        {
            throw new AggregateException(exceptions);
        }
    }

    /// <summary>Disposes each owned transaction after an outcome has been chosen.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        List<Exception>? errors = null;

        foreach (var txn in _txns)
        {
            try
            {
                await txn.DisposeAsync();
            }
            catch (Exception ex)
            {
                errors ??= [];
                errors.Add(ex);
            }
        }

        if (errors is not null)
        {
            if (errors.Count == 1)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
            }

            throw new AggregateException(errors);
        }

        if (!_isFinished)
        {
            throw new InvalidOperationException("UnitOfWork must be committed or rolled back before disposal.");
        }
    }
}
