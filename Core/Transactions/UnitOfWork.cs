namespace Brigade.Net.Core.Transactions;

/// <summary>
/// Coordinates commit/rollback across a group of <see cref="ITxn" /> instances.
/// Must be committed or rolled back before disposal.
/// </summary>
/// <param name="txns">The transactions to coordinate.</param>
public sealed class UnitOfWork(IEnumerable<ITxn> txns) : IDisposable
{
    private readonly List<ITxn> _txns = txns.ToList();
    private bool _isFinished;

    /// <summary>
    /// Adds a new <see cref="BasicTxn" /> built from the given delegates.
    /// </summary>
    /// <param name="commit">Invoked on commit, if provided.</param>
    /// <param name="rollback">Invoked on rollback, if provided.</param>
    /// <returns>This <see cref="UnitOfWork" />, for chaining.</returns>
    public UnitOfWork AddTxn(Func<Task>? commit = null, Func<Task>? rollback = null)
    {
        _txns.Add(new BasicTxn(commit, rollback));

        return this;
    }

    /// <summary>
    /// Commits every transaction. If any commit throws, rolls back every transaction
    /// and rethrows the original exception.
    /// </summary>
    public async Task CommitAsync()
    {
        try
        {
            foreach (var txn in _txns)
            {
                await txn.CommitAsync();
            }

            _isFinished = true;
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Rolls back every transaction. Each rollback runs in its own try/catch so a
    /// failure in one doesn't stop the rest; any failures are thrown together as an
    /// <see cref="AggregateException" />.
    /// </summary>
    public async Task RollbackAsync()
    {
        List<Exception>? exceptions = null;

        foreach (var txn in _txns)
        {
            try
            {
                await txn.RollbackAsync();
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_isFinished)
        {
            throw new InvalidOperationException("UnitOfWork must be committed or rolled back before disposal.");
        }
    }
}
