namespace Brigade.Net.Core.Transactions;

/// <summary>
/// An <see cref="ITxn" /> backed by optional commit/rollback delegates.
/// </summary>
/// <param name="commit">Invoked on <see cref="CommitAsync" />, if provided.</param>
/// <param name="rollback">Invoked on <see cref="RollbackAsync" />, if provided.</param>
public sealed class BasicTxn(
    Func<CancellationToken, Task>? commit = null,
    Func<CancellationToken, Task>? rollback = null
) : ITxn
{
    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (commit is not null)
        {
            await commit(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (rollback is not null)
        {
            await rollback(cancellationToken);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
