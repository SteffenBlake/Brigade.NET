using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie.Tests;

internal sealed class DisposalFaultTxn(Exception fault) : ITxn
{
    public int CommitCount { get; private set; }

    public int RollbackCount { get; private set; }

    public int DisposeCount { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCount++;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RollbackCount++;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return ValueTask.FromException(fault);
    }
}
