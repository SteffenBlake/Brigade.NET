using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Core.Tests;

internal sealed class TrackingTxn : ITxn
{
    public int DisposeCount { get; private set; }

    public CancellationToken CommitToken { get; private set; }

    public CancellationToken RollbackToken { get; private set; }

    public Exception? CommitException { get; set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitToken = cancellationToken;
        return CommitException is null ? Task.CompletedTask : Task.FromException(CommitException);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RollbackToken = cancellationToken;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return ValueTask.CompletedTask;
    }
}
