namespace Brigade.Net.Core.Transactions;

/// <summary>
/// A single unit of work that can be committed or rolled back.
/// </summary>
public interface ITxn
{
    /// <summary>
    /// Commits this transaction.
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Rolls back this transaction.
    /// </summary>
    Task RollbackAsync();
}
