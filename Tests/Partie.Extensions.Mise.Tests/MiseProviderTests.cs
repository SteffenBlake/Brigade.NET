using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.Tests;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Brigade.Net.Partie.Extensions.Mise.Tests;

public sealed class MiseProviderTests
{
    [Fact]
    public async Task ReaderOpensOnlyWhenUsedAndDisposesAfterFailure()
    {
        var path = NewPath();
        try
        {
            var context = new DbReaderProviderContext([Config(path)]);
            var unused = await DbReaderProvider<Unit, Unit>.OnQueryAsync(
                context,
                Unit.Default,
                _ => ValueTask.FromResult<Result<Unit>>(Unit.Default),
                default
            );
            Assert.True(unused.IsSuccess(out _));
            Assert.False(File.Exists(path));

            var failed = await DbReaderProvider<Unit, Unit>.OnQueryAsync(
                context,
                Unit.Default,
                async reader =>
                {
                    var value = await reader.ScalarAsync<long>(new SqlText("SELECT 1"));
                    Assert.True(value.IsSuccess(out var result));
                    Assert.Equal(1, result);
                    return new NotFound();
                },
                default
            );
            Assert.True(failed.IsNotFound(out _));
            Assert.True(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task WriterSharesLazyTransactionAndFollowsUnitOfWork(bool commit, int expectedRows)
    {
        var path = NewPath();
        try
        {
            await using (var setup = new SqliteConnection($"Data Source={path}"))
            {
                await setup.OpenAsync();
                await using var create = setup.CreateCommand();
                create.CommandText = "CREATE TABLE Items (Id INTEGER PRIMARY KEY)";
                await create.ExecuteNonQueryAsync();
            }

            var transactionContext = new DbWriterTxnProviderContext([Config(path)]);
            var outcome = await DbWriterTxnProvider<Unit, Unit>.OnCommandAsync(
                transactionContext,
                Unit.Default,
                async transaction =>
                {
                    var work = new UnitOfWork([transaction]);
                    try
                    {
                        var result = await DbWriterProvider<Unit, Unit>.OnCommandAsync(
                            new DbWriterProviderContext(transaction),
                            Unit.Default,
                            async writer =>
                            {
                                var written = await writer.ExecuteAsync(
                                    new SqlText("INSERT INTO Items (Id) VALUES (1)")
                                );
                                Assert.True(written.IsSuccess(out _));
                                return commit ? Unit.Default : new NotFound();
                            },
                            default
                        );
                        if (commit)
                        {
                            await work.CommitAsync();
                        }
                        else
                        {
                            await work.RollbackAsync();
                        }

                        return result;
                    }
                    finally
                    {
                        await work.DisposeAsync();
                    }
                },
                default
            );
            Assert.Equal(commit, outcome.IsSuccess(out _));

            await using var check = new SqliteConnection($"Data Source={path}");
            await check.OpenAsync();
            await using var count = check.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM Items";
            Assert.Equal(expectedRows, Convert.ToInt32(await count.ExecuteScalarAsync()));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task UnusedWriterStartsNoConnection()
    {
        var path = NewPath();
        try
        {
            var outcome = await DbWriterTxnProvider<Unit, Unit>.OnCommandAsync(
                new DbWriterTxnProviderContext([Config(path)]),
                Unit.Default,
                async transaction =>
                {
                    var work = new UnitOfWork([transaction]);
                    await work.CommitAsync();
                    await work.DisposeAsync();
                    return Unit.Default;
                },
                default
            );
            Assert.True(outcome.IsSuccess(out _));
            Assert.False(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ConfigProviderUsesNamedConnectionString()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = "Data Source=example.db" }
        ).Build();
        var services = new ServiceCollection()
            .AddKeyedSingleton<System.Data.Common.DbProviderFactory>("Sqlite", SqliteFactory.Instance)
            .BuildServiceProvider();
        var context = new DbConfigProviderContext(settings, services, "Sqlite");
        var result = await DbConfigProvider<Unit, Unit>.OnQueryAsync(
            context,
            Unit.Default,
            config =>
            {
                Assert.Equal("Data Source=example.db", config.ConnectionString);
                Assert.Same(SqliteFactory.Instance, config.ProviderFactory);
                return ValueTask.FromResult<Result<Unit>>(Unit.Default);
            },
            default
        );
        Assert.True(result.IsSuccess(out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReaderDisposesConnectionAfterThrowOrCancellation(bool canceled)
    {
        var connection = new FakeDbConnection { ScalarValue = 3L };
        var config = new DbRouteConfig("fake", new FakeDbProviderFactory(connection));
        Exception expected = canceled
            ? new OperationCanceledException("canceled")
            : new InvalidOperationException("failed");
        var calls = 0;

        var actual = await Record.ExceptionAsync(() =>
            DbReaderProvider<Unit, Unit>.OnQueryAsync(
                new DbReaderProviderContext([config]),
                Unit.Default,
                async reader =>
                {
                    calls++;
                    var scalar = await reader.ScalarAsync<long>(new SqlText("SELECT 3"));
                    Assert.True(scalar.IsSuccess(out var value));
                    Assert.Equal(3, value);
                    throw expected;
                },
                default
            ).AsTask()
        );

        Assert.Same(expected, actual);
        Assert.Equal(1, calls);
        Assert.Equal(1, connection.OpenCount);
        Assert.Equal(0, connection.BeginTransactionCount);
        Assert.Equal(1, connection.DisposeCount);
    }

    [Fact]
    public async Task NamedConfigsSelectDistinctFactories()
    {
        var first = new FakeDbProviderFactory(new FakeDbConnection());
        var second = new FakeDbProviderFactory(new FakeDbConnection());
        var settings = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sqlite"] = "first",
                ["ConnectionStrings:PostgreSql"] = "second"
            }
        ).Build();
        using var services = new ServiceCollection()
            .AddKeyedSingleton<System.Data.Common.DbProviderFactory>("Sqlite", first)
            .AddKeyedSingleton<System.Data.Common.DbProviderFactory>("PostgreSql", second)
            .BuildServiceProvider();

        var names = new[] { "Sqlite", "PostgreSql" };
        var configs = new List<IDbConfig>();
        foreach (var name in names)
        {
            await DbConfigProvider<Unit, Unit>.OnQueryAsync(
                new DbConfigProviderContext(settings, services, name),
                Unit.Default,
                config =>
                {
                    configs.Add(config);
                    return ValueTask.FromResult<Result<Unit>>(Unit.Default);
                },
                default
            );
        }

        Assert.Equal(["first", "second"], configs.Select(config => config.ConnectionString));
        Assert.Same(first, configs[0].ProviderFactory);
        Assert.Same(second, configs[1].ProviderFactory);
    }

    [Fact]
    public async Task MissingDatabaseConfigurationFailsWhenReaderOrWriterIsRequested()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DbReaderProvider<Unit, Unit>.OnQueryAsync(
                new DbReaderProviderContext([]), Unit.Default,
                _ => ValueTask.FromResult<Result<Unit>>(Unit.Default), default).AsTask());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DbWriterTxnProvider<Unit, Unit>.OnCommandAsync(
                new DbWriterTxnProviderContext([]), Unit.Default,
                transaction => DbWriterProvider<Unit, Unit>.OnCommandAsync(
                    new DbWriterProviderContext(transaction), Unit.Default,
                    _ => ValueTask.FromResult<Result<Unit>>(Unit.Default), default), default).AsTask());
    }

    [Fact]
    public async Task WriterProviderRejectsUnrelatedTransaction()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DbWriterProvider<Unit, Unit>.OnCommandAsync(
                new DbWriterProviderContext(new BasicTxn()), Unit.Default,
                _ => ValueTask.FromResult<Result<Unit>>(Unit.Default), default).AsTask());
    }

    [Fact]
    public async Task CommandConfigProviderUsesNamedConnectionString()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = "Data Source=command.db" }
        ).Build();
        using var services = new ServiceCollection()
            .AddKeyedSingleton<System.Data.Common.DbProviderFactory>("Sqlite", SqliteFactory.Instance)
            .BuildServiceProvider();
        var result = await DbConfigProvider<Unit, Unit>.OnCommandAsync(
            new DbConfigProviderContext(settings, services, "Sqlite"), Unit.Default,
            config =>
            {
                Assert.Equal("Data Source=command.db", config.ConnectionString);
                return ValueTask.FromResult<Result<Unit>>(Unit.Default);
            }, default);
        Assert.True(result.IsSuccess(out _));
    }

    [Fact]
    public void ConfigProviderRejectsMissingConnectionString()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var context = new DbConfigProviderContext(new ConfigurationBuilder().Build(), services, "Missing");
        Assert.Throws<InvalidOperationException>(() => context.CreateConfig());
    }

    [Fact]
    public async Task TransactionDisposalIsIdempotent()
    {
        var transaction = new DbWriterTxn(null);
        await transaction.DisposeAsync();
        await transaction.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => transaction.Writer);
    }

    private static DbRouteConfig Config(string path) =>
        new($"Data Source={path}", SqliteFactory.Instance);

    private static string NewPath() => Path.Combine(Path.GetTempPath(), $"mise-{Guid.NewGuid():N}.db");

    private sealed record SqlText(string Text) : IQueryBuilder, ICommandBuilder
    {
        public CompiledSql Compile() => new(Text);
    }
}
