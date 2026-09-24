using System.Data;
using System.Data.Common;

namespace Brigade.Net.Mise.Tests;

internal sealed class FakeDbTransaction(
    FakeDbConnection connection,
    IsolationLevel isolationLevel
) : DbTransaction
{
    public override IsolationLevel IsolationLevel => isolationLevel;

    protected override DbConnection DbConnection => connection;

    public int CommitCount { get; private set; }

    public int RollbackCount { get; private set; }

    public int DisposeCount { get; private set; }

    public CancellationToken CommitToken { get; private set; }

    public CancellationToken RollbackToken { get; private set; }

    public Exception? CommitException { get; set; }

    public Exception? RollbackException { get; set; }

    public override void Commit()
    {
        connection.LifecycleEvents.Add("transaction.commit");
        CommitCount++;
        if (CommitException is not null)
        {
            throw CommitException;
        }
    }

    public override void Rollback()
    {
        connection.LifecycleEvents.Add("transaction.rollback");
        RollbackCount++;
        if (RollbackException is not null)
        {
            throw RollbackException;
        }
    }

    public override Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitToken = cancellationToken;
        return Task.Run(Commit, cancellationToken);
    }

    public override Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RollbackToken = cancellationToken;
        return Task.Run(Rollback, cancellationToken);
    }

    public override ValueTask DisposeAsync()
    {
        connection.LifecycleEvents.Add("transaction.dispose");
        DisposeCount++;
        return ValueTask.CompletedTask;
    }
}
