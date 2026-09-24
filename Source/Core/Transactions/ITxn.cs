namespace Brigade.Net.Core.Transactions;

/// <summary>
/// A single unit of work that can be committed or rolled back.
/// </summary>
public interface ITxn : IAsyncDisposable
{
    /// <summary>
    /// Commits this transaction.
    /// </summary>
    /// <param name="cancellationToken">The route or caller cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back this transaction.
    /// </summary>
    /// <param name="cancellationToken">The route or caller cancellation token.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
