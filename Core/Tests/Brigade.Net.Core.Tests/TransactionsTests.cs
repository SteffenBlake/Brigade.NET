using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Core.Tests;

public class TransactionsTests
{
    [Fact]
    public async Task BasicTxn_CommitAsync_InvokesDelegate()
    {
        var invoked = false;
        var txn = new BasicTxn(commit: () =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        await txn.CommitAsync();

        Assert.True(invoked);
    }

    [Fact]
    public async Task BasicTxn_CommitAsync_NullDelegate_NoOp()
    {
        var txn = new BasicTxn();

        await txn.CommitAsync();
    }

    [Fact]
    public async Task BasicTxn_RollbackAsync_InvokesDelegate()
    {
        var invoked = false;
        var txn = new BasicTxn(rollback: () =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        await txn.RollbackAsync();

        Assert.True(invoked);
    }

    [Fact]
    public async Task BasicTxn_RollbackAsync_NullDelegate_NoOp()
    {
        var txn = new BasicTxn();

        await txn.RollbackAsync();
    }

    [Fact]
    public void AddTxn_ReturnsSameInstance_ForChaining()
    {
        var uow = new UnitOfWork([]);

        var chained = uow.AddTxn();

        Assert.Same(uow, chained);
    }

    [Fact]
    public async Task CommitAsync_InvokesCommitOnEveryTxn()
    {
        var committed = new List<int>();
        var uow = new UnitOfWork([])
            .AddTxn(commit: () =>
            {
                committed.Add(1);
                return Task.CompletedTask;
            })
            .AddTxn(commit: () =>
            {
                committed.Add(2);
                return Task.CompletedTask;
            });

        await uow.CommitAsync();

        Assert.Equal([1, 2], committed);
        uow.Dispose();
    }

    [Fact]
    public async Task CommitAsync_WhenATxnThrows_RollsBackAllAndRethrows()
    {
        var rolledBack = new List<int>();
        var uow = new UnitOfWork([])
            .AddTxn(
                commit: () => throw new InvalidOperationException("commit failed"),
                rollback: () =>
                {
                    rolledBack.Add(1);
                    return Task.CompletedTask;
                })
            .AddTxn(rollback: () =>
            {
                rolledBack.Add(2);
                return Task.CompletedTask;
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());

        Assert.Equal("commit failed", ex.Message);
        Assert.Equal([1, 2], rolledBack);
        uow.Dispose();
    }

    [Fact]
    public async Task RollbackAsync_InvokesRollbackOnEveryTxn()
    {
        var rolledBack = new List<int>();
        var uow = new UnitOfWork([])
            .AddTxn(rollback: () =>
            {
                rolledBack.Add(1);
                return Task.CompletedTask;
            })
            .AddTxn(rollback: () =>
            {
                rolledBack.Add(2);
                return Task.CompletedTask;
            });

        await uow.RollbackAsync();

        Assert.Equal([1, 2], rolledBack);
        uow.Dispose();
    }

    [Fact]
    public async Task RollbackAsync_WhenATxnThrows_StillRollsBackRestAndAggregates()
    {
        var rolledBack = new List<int>();
        var uow = new UnitOfWork([])
            .AddTxn(rollback: () => throw new InvalidOperationException("rollback 1 failed"))
            .AddTxn(rollback: () =>
            {
                rolledBack.Add(2);
                return Task.CompletedTask;
            })
            .AddTxn(rollback: () => throw new InvalidOperationException("rollback 3 failed"));

        var ex = await Assert.ThrowsAsync<AggregateException>(() => uow.RollbackAsync());

        Assert.Equal(2, ex.InnerExceptions.Count);
        Assert.Equal([2], rolledBack);
        uow.Dispose();
    }

    [Fact]
    public void Dispose_WithoutCommitOrRollback_Throws()
    {
        var uow = new UnitOfWork([]);

        Assert.Throws<InvalidOperationException>(uow.Dispose);
    }

    [Fact]
    public async Task Dispose_AfterCommit_DoesNotThrow()
    {
        var uow = new UnitOfWork([]);

        await uow.CommitAsync();

        uow.Dispose();
    }

    [Fact]
    public async Task Dispose_AfterRollback_DoesNotThrow()
    {
        var uow = new UnitOfWork([]);

        await uow.RollbackAsync();

        uow.Dispose();
    }
}
