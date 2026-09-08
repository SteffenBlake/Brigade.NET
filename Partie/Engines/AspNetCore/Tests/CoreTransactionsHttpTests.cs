using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class CoreTransactionsHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneratedRoute_CompletesUnitOfWork(bool rollback)
    {
        var events = new List<string>();
        var body = await CoreHttpScenario.Run(async () =>
        {
            using var work = new UnitOfWork([]);
            work.AddTxn();
            for (var index = 0; index < 2; index++)
            {
                var transaction = index;
                work.AddTxn(
                    commit: () => { events.Add("commit:" + transaction); return Task.CompletedTask; },
                    rollback: () => { events.Add("rollback:" + transaction); return Task.CompletedTask; }
                );
            }
            if (rollback)
            {
                await work.RollbackAsync();
            }
            else
            {
                await work.CommitAsync();
            }
            return new Success<object?>("done");
        });

        Assert.Equal("\"done\"", body);
        var action = rollback ? "rollback:" : "commit:";
        Assert.Equal([action + "0", action + "1"], events);
    }

    [Fact]
    public async Task GeneratedRoute_RollsBackAllTransactionsOnCommitFailure()
    {
        var events = new List<string>();
        var body = await CoreHttpScenario.Run(async () =>
        {
            using var work = new UnitOfWork([])
                .AddTxn(
                    commit: () => throw new InvalidOperationException("commit failed"),
                    rollback: () => { events.Add("first"); return Task.CompletedTask; }
                )
                .AddTxn(rollback: () => { events.Add("second"); return Task.CompletedTask; });
            try
            {
                await work.CommitAsync();
                return new Success<object?>("unexpected commit");
            }
            catch (InvalidOperationException exception)
            {
                return new Conflict(exception.Message);
            }
        });

        Assert.Equal("{\"message\":\"commit failed\"}", body);
        Assert.Equal(["first", "second"], events);
    }

    [Fact]
    public async Task GeneratedRoute_AggregatesRollbackFailuresAndContinues()
    {
        var rolledBack = false;
        var body = await CoreHttpScenario.Run(async () =>
        {
            using var work = new UnitOfWork([])
                .AddTxn(rollback: () => throw new InvalidOperationException("first"))
                .AddTxn(rollback: () => { rolledBack = true; return Task.CompletedTask; })
                .AddTxn(rollback: () => throw new InvalidOperationException("last"));
            try
            {
                await work.RollbackAsync();
                return new Success<object?>("unexpected rollback");
            }
            catch (AggregateException exception)
            {
                return new Success<object?>(exception.InnerExceptions.Select(failure => failure.Message).ToArray());
            }
        });

        Assert.True(rolledBack);
        Assert.Equal("[\"first\",\"last\"]", body);
    }

    [Fact]
    public async Task GeneratedRoute_RejectsUnfinishedUnitOfWork()
    {
        var body = await CoreHttpScenario.Run(async () =>
        {
            var work = new UnitOfWork([]);
            try
            {
                work.Dispose();
                return new Success<object?>("unexpected disposal");
            }
            catch (InvalidOperationException exception)
            {
                return new Conflict(exception.Message);
            }
            finally
            {
                await work.RollbackAsync();
                work.Dispose();
            }
        });

        Assert.Contains("UnitOfWork must be committed or rolled back before disposal.", body);
    }
}