namespace Brigade.Net.Core.Transactions;

/// <summary>
/// An <see cref="ITxn" /> backed by optional commit/rollback delegates.
/// </summary>
/// <param name="commit">Invoked on <see cref="CommitAsync" />, if provided.</param>
/// <param name="rollback">Invoked on <see cref="RollbackAsync" />, if provided.</param>
public sealed class BasicTxn(Func<Task>? commit = null, Func<Task>? rollback = null) : ITxn
{
    /// <inheritdoc />
    public async Task CommitAsync()
    {
        if (commit is not null)
        {
            await commit();
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync()
    {
        if (rollback is not null)
        {
            await rollback();
        }
    }
}
