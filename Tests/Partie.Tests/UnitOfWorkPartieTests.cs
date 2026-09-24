using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie.Tests;

public sealed class UnitOfWorkPartieTests
{
    [Fact]
    public async Task DownstreamFaultRemainsPrimaryWhenRollbackFails()
    {
        var expected = new InvalidOperationException("downstream failed");
        var transaction = new BasicTxn(
            rollback: _ => throw new InvalidOperationException("rollback failed")
        );

        var actual = await Record.ExceptionAsync(() =>
            UnitOfWorkPartie<Unit, int>.OnCommandAsync(
                new UnitOfWorkContext([transaction]),
                new Unit(),
                _ => ValueTask.FromException<Result<int>>(expected),
                CancellationToken.None
            ).AsTask()
        );

        Assert.Same(expected, actual);
        Assert.IsType<AggregateException>(actual.Data["UnitOfWork.RollbackException"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RouteTokenReachesTransaction(bool success)
    {
        using var source = new CancellationTokenSource();
        CancellationToken seen = default;
        var transaction = new BasicTxn(
            commit: token =>
            {
                seen = token;
                return Task.CompletedTask;
            },
            rollback: token =>
            {
                seen = token;
                return Task.CompletedTask;
            }
        );
        Result<int> result = success ? 1 : new Conflict("busy");

        await UnitOfWorkPartie<Unit, int>.OnCommandAsync(
            new UnitOfWorkContext([transaction]),
            new Unit(),
            _ => ValueTask.FromResult(result),
            source.Token
        );

        if (success)
        {
            Assert.Equal(source.Token, seen);
        }
        else
        {
            Assert.NotEqual(source.Token, seen);
            Assert.True(seen.CanBeCanceled);
            Assert.False(seen.IsCancellationRequested);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessAndDeprecatedCommitAndPreserveResult(bool deprecated)
    {
        var commits = 0;
        var rollbacks = 0;
        var context = new UnitOfWorkContext(
            [new BasicTxn(
                commit: _ =>
        {
            commits++;
            return Task.CompletedTask;
        },
                rollback: _ =>
        {
            rollbacks++;
            return Task.CompletedTask;
        }
            )]
        );
        Result<int> expected = deprecated ? new Deprecated<int>(42, DateTime.UnixEpoch, "old") : 42;
        var actual = await UnitOfWorkPartie<Unit, int>.OnCommandAsync(context, new Unit(), work => ValueTask.FromResult(expected), CancellationToken.None);
        Assert.Same(expected, actual);
        Assert.Equal(1, commits);
        Assert.Equal(0, rollbacks);
    }

    [Fact]
    public async Task FailureRollsBackAndPreservesResult()
    {
        var rollbacks = 0;
        var context = new UnitOfWorkContext(
            [new BasicTxn(
                commit: _ => throw new InvalidOperationException("Must not commit"),
                rollback: _ =>
        {
            rollbacks++;
            return Task.CompletedTask;
        }
            )]
        );
        Result<int> expected = new Conflict("busy");
        var actual = await UnitOfWorkPartie<Unit, int>.OnCommandAsync(context, new Unit(), work => ValueTask.FromResult(expected), CancellationToken.None);
        Assert.Same(expected, actual);
        Assert.Equal(1, rollbacks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownstreamExceptionOrCancellationRollsBack(bool cancelled)
    {
        var rollbacks = 0;
        var context = new UnitOfWorkContext(
            [new BasicTxn(
                rollback: _ =>
        {
            rollbacks++;
            return Task.CompletedTask;
        }
            )]
        );
        Exception expected = cancelled ? new OperationCanceledException() : new InvalidOperationException("failed");
        var actual = await Record.ExceptionAsync(
            () => UnitOfWorkPartie<Unit, int>.OnCommandAsync(
                context,
                new Unit(),
                work => ValueTask.FromException<Result<int>>(expected),
                CancellationToken.None
            ).AsTask()
        );
        Assert.Same(expected, actual);
        Assert.Equal(1, rollbacks);
    }

    [Fact]
    public async Task CommitFailureRollsBackExactlyOnce()
    {
        var rollbacks = 0;
        var expected = new InvalidOperationException("commit failed");
        var context = new UnitOfWorkContext(
            [new BasicTxn(
                commit: _ => throw expected,
                rollback: _ =>
        {
            rollbacks++;
            return Task.CompletedTask;
        }
            )]
        );
        var actual = await Record.ExceptionAsync(
            () => UnitOfWorkPartie<Unit, int>.OnCommandAsync(
                context,
                new Unit(),
                work => ValueTask.FromResult<Result<int>>(1),
                CancellationToken.None
            ).AsTask()
        );
        Assert.Same(expected, actual);
        Assert.Equal(1, rollbacks);
    }

    [Fact]
    public async Task EmptyUnitOfWorkAllowsAddingTransactionDownstream()
    {
        var committed = false;
        await UnitOfWorkPartie<Unit, int>.OnCommandAsync(
            new UnitOfWorkContext([]),
            new Unit(),
            work =>
        {
            work.AddTxn(
                    commit: _ =>
            {
                committed = true;
                return Task.CompletedTask;
            }
                );
            return ValueTask.FromResult<Result<int>>(1);
        },
            CancellationToken.None
        );
        Assert.True(committed);
    }
}
