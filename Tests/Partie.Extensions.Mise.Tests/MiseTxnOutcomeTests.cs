using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.Tests;

namespace Brigade.Net.Partie.Extensions.Mise.Tests;

public sealed class MiseTxnOutcomeTests
{
    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(2, 0, 1)]
    [InlineData(3, 0, 1)]
    [InlineData(4, 0, 1)]
    [InlineData(5, 0, 1)]
    [InlineData(6, 0, 1)]
    [InlineData(7, 0, 1)]
    public async Task RealUnitOfWorkChoosesOneOutcome(int outcome, int commits, int rollbacks)
    {
        var connection = new FakeDbConnection();
        var transaction = new MiseWriterTransaction(Config(connection));

        var result = await UnitOfWorkPartie<Unit, Unit>.OnCommandAsync(
            new UnitOfWorkContext([transaction]),
            Unit.Default,
            async _ =>
            {
                var written = await MiseWriterProvider<Unit, Unit>.OnCommandAsync(
                    new MiseWriterProviderContext(transaction),
                    Unit.Default,
                    async writer =>
                    {
                        await writer.ExecuteAsync(new SqlText("UPDATE items SET value = 1"));
                        return Outcome(outcome);
                    },
                    default
                );
                return written;
            },
            default
        );

        Assert.Equal(outcome is 0 or 1, result.IsSuccess(out _) || result.IsDeprecated(out _));
        Assert.Equal(1, connection.OpenCount);
        Assert.Equal(1, connection.BeginTransactionCount);
        Assert.Equal(1, connection.DisposeCount);
        Assert.Equal(commits, connection.LastTransaction!.CommitCount);
        Assert.Equal(rollbacks, connection.LastTransaction.RollbackCount);
        Assert.Equal(1, connection.LastTransaction.DisposeCount);
        Assert.Same(connection.LastTransaction, connection.LastCommand!.AttachedTransaction);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownstreamThrowOrCancellationRollsBackAndRethrows(bool canceled)
    {
        var connection = new FakeDbConnection();
        var transaction = new MiseWriterTransaction(Config(connection));
        Exception expected = canceled
            ? new OperationCanceledException("canceled")
            : new InvalidOperationException("failed");

        var actual = await Record.ExceptionAsync(() =>
            UnitOfWorkPartie<Unit, Unit>.OnCommandAsync(
                new UnitOfWorkContext([transaction]),
                Unit.Default,
                async _ =>
                {
                    await transaction.Writer.ExecuteAsync(new SqlText("UPDATE items SET value = 1"));
                    throw expected;
                },
                default
            ).AsTask()
        );

        Assert.Same(expected, actual);
        Assert.Equal(0, connection.LastTransaction!.CommitCount);
        Assert.Equal(1, connection.LastTransaction.RollbackCount);
        Assert.Equal(1, connection.DisposeCount);
    }

    [Fact]
    public async Task CommitFailureRollsBackOnceAndDisposes()
    {
        var connection = new FakeDbConnection();
        var transaction = new MiseWriterTransaction(Config(connection));
        var expected = new InvalidOperationException("commit failed");

        var actual = await Record.ExceptionAsync(() =>
            UnitOfWorkPartie<Unit, Unit>.OnCommandAsync(
                new UnitOfWorkContext([transaction]),
                Unit.Default,
                async _ =>
                {
                    await transaction.Writer.ExecuteAsync(new SqlText("UPDATE items SET value = 1"));
                    connection.LastTransaction!.CommitException = expected;
                    return Unit.Default;
                },
                default
            ).AsTask()
        );

        Assert.Same(expected, actual);
        Assert.Equal(1, connection.LastTransaction!.CommitCount);
        Assert.Equal(1, connection.LastTransaction.RollbackCount);
        Assert.Equal(1, connection.LastTransaction.DisposeCount);
        Assert.Equal(1, connection.DisposeCount);
    }

    [Fact]
    public async Task ShortCircuitBeforeWriterOpensNoResource()
    {
        var connection = new FakeDbConnection();
        var transaction = new MiseWriterTransaction(Config(connection));
        var result = await UnitOfWorkPartie<Unit, Unit>.OnCommandAsync(
            new UnitOfWorkContext([transaction]),
            Unit.Default,
            _ => ValueTask.FromResult<Result<Unit>>(new Conflict("stopped")),
            default
        );

        Assert.True(result.IsConflict(out _));
        Assert.Equal(0, connection.OpenCount);
        Assert.Equal(0, connection.BeginTransactionCount);
        Assert.Equal(0, connection.DisposeCount);
    }

    private static MiseRouteConfig Config(FakeDbConnection connection) =>
        new("fake", new FakeDbProviderFactory(connection));

    private static Result<Unit> Outcome(int index) => index switch
    {
        0 => Unit.Default,
        1 => new Deprecated<Unit>(Unit.Default, DateTime.UnixEpoch, "old"),
        2 => new Error("type", "title"),
        3 => new NotFound(),
        4 => new Conflict(),
        5 => new Forbidden(),
        6 => new GatewayError(),
        7 => new TimeoutResult(),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    private sealed record SqlText(string Text) : ICommandBuilder
    {
        public CompiledSql Compile() => new(Text);
    }
}
