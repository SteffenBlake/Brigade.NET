using System.Data;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

public sealed class ExecutionContractTests
{
    [Fact]
    public async Task WriterUsesOneLazyTransactionAndUnitOfWorkDisposesIt()
    {
        var connection = new FakeDbConnection { NonQueryResult = 3 };
        await using var writer = new DbWriter(connection: connection);
        var transaction = writer.Transaction;
        Assert.Equal(0, connection.BeginTransactionCount);
        await using (var work = new Brigade.Net.Core.Transactions.UnitOfWork([transaction]))
        {
            var result = await writer.ExecuteAsync(Query());
            Assert.True(result.IsSuccess(out var count));
            Assert.Equal(3, count);
            Assert.Equal(1, connection.BeginTransactionCount);
            Assert.Same(connection.LastTransaction, connection.LastCommand!.AttachedTransaction);
            await work.CommitAsync();
        }

        Assert.Equal(1, connection.LastTransaction!.CommitCount);
        Assert.Equal(0, connection.LastTransaction.RollbackCount);
        Assert.Equal(1, connection.LastTransaction.DisposeCount);
        await transaction.DisposeAsync();
        Assert.Equal(1, connection.LastTransaction.DisposeCount);
    }

    [Fact]
    public async Task CompletedTransactionDoesNotCompleteProviderTwice()
    {
        var connection = new FakeDbConnection();
        await using var writer = new DbWriter(connection: connection);
        var adapter = writer.Transaction;
        await writer.ExecuteAsync(Query());

        await adapter.CommitAsync();
        await adapter.CommitAsync();
        await adapter.RollbackAsync();
        await adapter.DisposeAsync();

        Assert.Equal(1, connection.LastTransaction!.CommitCount);
        Assert.Equal(0, connection.LastTransaction.RollbackCount);
        Assert.Equal(1, connection.LastTransaction.DisposeCount);
    }

    [Fact]
    public async Task ReaderAndTransactionResourcesDisposeInOrder()
    {
        var connection = new FakeDbConnection();
        connection.Rows.Add([1, "one"]);
        var factory = new FakeDbProviderFactory(connection);
        await using (var writer = new DbWriter(config: new TestConfig("secret", factory)))
        {
            await using (var work = new Brigade.Net.Core.Transactions.UnitOfWork([writer.Transaction]))
            {
                await writer.ListAsync<TestRow>(Query());
                await work.CommitAsync();
            }
        }

        Assert.Equal(
            ["connection.open", "transaction.begin", "reader.dispose", "command.dispose",
                "transaction.commit", "transaction.dispose", "connection.dispose"],
            connection.LifecycleEvents
        );
    }

    [Fact]
    public async Task WriterCannotDisposeBeforeItsTransactionAdapter()
    {
        var connection = new FakeDbConnection();
        var writer = new DbWriter(connection: connection);
        var adapter = writer.Transaction;
        await writer.ExecuteAsync(Query());

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.DisposeAsync().AsTask());
        await adapter.RollbackAsync();
        await adapter.DisposeAsync();
        await writer.DisposeAsync();

        Assert.Equal(1, connection.LastTransaction!.DisposeCount);
    }

    [Fact]
    public async Task ExplicitBeginReturnsSameAdapterAndStartsOnce()
    {
        var connection = new FakeDbConnection();
        await using var writer = new DbWriter(connection: connection);
        using var source = new CancellationTokenSource();

        var first = await writer.BeginTransactionAsync(source.Token);
        var second = writer.Transaction;
        await writer.BeginTransactionAsync(source.Token);
        await first.CommitAsync(source.Token);
        await first.DisposeAsync();

        Assert.Same(first, second);
        Assert.Equal(1, connection.BeginTransactionCount);
        Assert.Equal(source.Token, connection.LastBeginTransactionToken);
        Assert.Equal(source.Token, connection.LastTransaction!.CommitToken);
        Assert.Equal(1, connection.LastTransaction.DisposeCount);
    }

    [Fact]
    public async Task UnusedTransactionAdapterOpensNoConnection()
    {
        var connection = new FakeDbConnection();
        var factory = new FakeDbProviderFactory(connection);
        await using var writer = new DbWriter(config: new TestConfig("secret", factory));
        var adapter = writer.Transaction;

        await adapter.CommitAsync();
        await adapter.DisposeAsync();

        Assert.Equal(0, connection.OpenCount);
        Assert.Equal(0, connection.BeginTransactionCount);
    }

    [Fact]
    public async Task WrappedTransactionRollsBackAndKeepsCallerConnection()
    {
        var connection = new FakeDbConnection();
        await connection.OpenAsync();
        var providerTransaction = Assert.IsType<FakeDbTransaction>(await connection.BeginTransactionAsync());
        await using (var writer = new DbWriter(transaction: providerTransaction))
        {
            var adapter = writer.Transaction;
            await writer.ExecuteAsync(Query());
            Assert.Same(providerTransaction, connection.LastCommand!.AttachedTransaction);
            using var source = new CancellationTokenSource();
            await adapter.RollbackAsync(source.Token);
            Assert.Equal(source.Token, providerTransaction.RollbackToken);
            await adapter.DisposeAsync();
            await adapter.RollbackAsync();
        }

        Assert.Equal(1, providerTransaction.RollbackCount);
        Assert.Equal(1, providerTransaction.DisposeCount);
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task UnusedWrappedTransactionIsDisposedByWriter()
    {
        var connection = new FakeDbConnection();
        await connection.OpenAsync();
        var providerTransaction = Assert.IsType<FakeDbTransaction>(await connection.BeginTransactionAsync());
        var writer = new DbWriter(transaction: providerTransaction);

        await writer.DisposeAsync();

        Assert.Equal(1, providerTransaction.DisposeCount);
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task CommitFailureLeavesTransactionForRollback()
    {
        var expected = new InvalidOperationException("commit failed");
        var connection = new FakeDbConnection();
        await using var writer = new DbWriter(connection: connection);
        var adapter = writer.Transaction;
        await writer.ExecuteAsync(Query());
        connection.LastTransaction!.CommitException = expected;

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.CommitAsync());
        Assert.Same(expected, actual);
        await adapter.RollbackAsync();
        await adapter.DisposeAsync();

        Assert.Equal(1, connection.LastTransaction.RollbackCount);
        Assert.Equal(1, connection.LastTransaction.DisposeCount);
    }

    [Fact]
    public async Task RollbackFailureEscapesAndTransactionIsDisposedOnce()
    {
        var expected = new InvalidOperationException("rollback failed");
        var connection = new FakeDbConnection();
        await using var writer = new DbWriter(connection: connection);
        var adapter = writer.Transaction;
        await writer.ExecuteAsync(Query());
        connection.LastTransaction!.RollbackException = expected;

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.RollbackAsync());
        Assert.Same(expected, actual);
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        Assert.Equal(1, connection.LastTransaction.RollbackCount);
        Assert.Equal(1, connection.LastTransaction.DisposeCount);
    }

    [Fact]
    public async Task ActiveWriteRejectsTransactionCompletion()
    {
        var connection = new FakeDbConnection { ExecuteGate = new TaskCompletionSource() };
        await using var writer = new DbWriter(connection: connection);
        var adapter = writer.Transaction;
        var running = writer.ExistsAsync(Query());

        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.CommitAsync());
        connection.ExecuteGate.SetResult();
        await running;
        await adapter.RollbackAsync();
        await adapter.DisposeAsync();
    }

    [Fact]
    public async Task WriterReturningShapesMapRowsAndScalar()
    {
        var connection = new FakeDbConnection { ScalarValue = 19 };
        connection.Rows.Add([1, "one"]);
        connection.Rows.Add([2, "two"]);
        await using var writer = new DbWriter(connection: connection);

        var scalar = await writer.ExecuteScalarAsync<int>(Query());
        var rows = await writer.ReturningListAsync<TestRow>(Query());
        var first = await writer.ReturningFirstOrNotFoundAsync<TestRow>(Query());

        Assert.True(scalar.IsSuccess(out var value));
        Assert.Equal(19, value);
        Assert.True(rows.IsSuccess(out var list));
        Assert.Equal(2, list.Count);
        Assert.True(first.IsSuccess(out var firstRow));
        Assert.Equal(new TestRow(1, "one"), firstRow);
    }

    [Fact]
    public async Task StoredProcedureTransfersCommandTypeAndParameters()
    {
        var connection = new FakeDbConnection { NonQueryResult = 4 };
        await using var writer = new DbWriter(connection: connection);
        var command = new CommandBuilder().Procedure("write_items")
            .ProcedureParameter("@name", "safe", DbType.String);

        var result = await writer.ExecuteAsync(command);

        Assert.True(result.IsSuccess(out var count));
        Assert.Equal(4, count);
        Assert.Equal(CommandType.StoredProcedure, connection.LastCommand!.CommandType);
        Assert.Equal("write_items", connection.LastCommand.CommandText);
        var parameter = Assert.IsType<FakeDbParameter>(connection.LastCommand.Parameters[0]);
        Assert.Equal("@name", parameter.ParameterName);
        Assert.Equal("safe", parameter.Value);
        Assert.Equal(DbType.String, parameter.DbType);
    }

    [Fact]
    public async Task StreamOwnsResourcesUntilEarlyBreak()
    {
        var connection = new FakeDbConnection();
        connection.Rows.Add([1, "one"]);
        connection.Rows.Add([2, "two"]);
        await using var db = new DbReader(connection: connection);

        var stream = db.StreamAsync<TestRow>(Query());
        Assert.Null(connection.LastCommand);

        await foreach (var row in stream)
        {
            Assert.Equal(1, row.Id);
            break;
        }

        Assert.True(connection.LastCommand!.IsDisposed);
        Assert.True(connection.LastCommand.LastReader!.IsClosed);
        Assert.Equal(1, connection.LastCommand.LastReader.DisposeCount);
    }

    [Fact]
    public async Task NeverEnumeratedStreamCreatesNoCommand()
    {
        var connection = new FakeDbConnection();
        await using var reader = new DbReader(connection: connection);

        var stream = reader.StreamAsync<TestRow>(Query());
        Assert.NotNull(stream);
        Assert.Null(connection.LastCommand);
        Assert.Equal(0, connection.OpenCount);
    }

    [Fact]
    public async Task StreamDisposesAfterFullReadAndConsumerException()
    {
        var connection = new FakeDbConnection();
        connection.Rows.Add([1, "one"]);
        await using var db = new DbReader(connection: connection);
        var rows = new List<TestRow>();

        await foreach (var row in db.StreamAsync<TestRow>(Query()))
        {
            rows.Add(row);
        }

        Assert.Equal([new TestRow(1, "one")], rows);
        Assert.True(connection.LastCommand!.IsDisposed);
        var expected = new InvalidOperationException("consumer fault");
        var actual = await Record.ExceptionAsync(async () =>
        {
            await foreach (var row in db.StreamAsync<TestRow>(Query()))
            {
                throw expected;
            }
        });
        Assert.Same(expected, actual);
        Assert.True(connection.LastCommand!.IsDisposed);
        Assert.True(connection.LastCommand.LastReader!.IsClosed);
    }

    [Fact]
    public async Task StreamCancellationReleasesOperation()
    {
        var connection = new FakeDbConnection();
        await using var db = new DbReader(connection: connection);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in db.StreamAsync<TestRow>(Query(), source.Token))
            {
            }
        });

        await db.ExistsAsync(Query());
        Assert.True(connection.LastCommand!.IsDisposed);
    }

    [Fact]
    public async Task StreamCancellationAfterFirstRowDisposesReaderAndCommand()
    {
        var connection = new FakeDbConnection();
        connection.Rows.Add([1, "one"]);
        connection.Rows.Add([2, "two"]);
        await using var db = new DbReader(connection: connection);
        using var source = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in db.StreamAsync<TestRow>(Query(), source.Token))
            {
                source.Cancel();
            }
        });

        Assert.True(connection.LastCommand!.IsDisposed);
        Assert.True(connection.LastCommand.LastReader!.IsClosed);
    }

    [Fact]
    public async Task ConfigPathOwnsConnectionCommandAndReader()
    {
        var connection = new FakeDbConnection();
        connection.Rows.Add([1, "one"]);
        connection.Rows.Add([2, null]);
        var factory = new FakeDbProviderFactory(connection);
        var query = QueryWithParameter();
        TestRow.BindCount = 0;
        await using (var reader = new DbReader(new TestConfig("secret", factory)))
        {
            var result = await reader.ListAsync<TestRow>(query);

            Assert.True(result.IsSuccess(out var success));
            Assert.Equal([new TestRow(1, "one"), new TestRow(2, null)], success);
            Assert.Equal(1, TestRow.BindCount);
            Assert.Equal(1, connection.OpenCount);
            Assert.Equal("secret", connection.ConnectionString);
            Assert.True(connection.LastCommand!.IsDisposed);
            Assert.True(connection.LastCommand.LastReader!.IsClosed);
            Assert.Equal(1, connection.LastCommand.DisposeCount);
            Assert.Equal(1, connection.LastCommand.LastReader.DisposeCount);
            Assert.Equal("select", connection.LastCommand.CommandText);
            Assert.Equal(19, connection.LastCommand.CommandTimeout);
            Assert.Equal(CommandBehavior.SingleResult, connection.LastCommand.LastBehavior);
            var parameter = Assert.IsType<FakeDbParameter>(connection.LastCommand.Parameters[0]);
            Assert.Equal("@id", parameter.ParameterName);
            Assert.Equal(7, parameter.Value);
            Assert.Equal(DbType.Int32, parameter.DbType);
        }
        Assert.Equal(1, connection.DisposeCount);
        Assert.Equal(1, factory.ParameterCreateCount);
    }

    [Fact]
    public async Task ExistingConnectionIsNeverClosedOrDisposed()
    {
        var connection = new FakeDbConnection { ScalarValue = 42 };
        await using (var reader = new DbReader(connection: connection))
        {
            var result = await reader.ScalarAsync<int>(Query());
            Assert.True(result.IsSuccess(out var success));
            Assert.Equal(42, success);
        }

        Assert.Equal(1, connection.OpenCount);
        Assert.Equal(0, connection.CloseCount);
        Assert.Equal(0, connection.DisposeCount);
        Assert.True(connection.LastCommand!.IsDisposed);
    }

    [Fact]
    public async Task FirstOrNotFoundReturnsNotFoundWithoutReadingAnotherRow()
    {
        var connection = new FakeDbConnection();
        await using var reader = new DbReader(connection: connection);

        var result = await reader.FirstOrNotFoundAsync<TestRow>(Query());

        Assert.True(result.IsNotFound(out _));
        Assert.True(connection.LastCommand!.LastReader!.IsClosed);
    }

    [Fact]
    public async Task FirstReturnsFirstOfManyAndBindsOnce()
    {
        var connection = new FakeDbConnection();
        connection.Rows.Add([1, "one"]);
        connection.Rows.Add([2, "two"]);
        TestRow.BindCount = 0;
        await using var reader = new DbReader(connection: connection);

        var result = await reader.FirstOrNotFoundAsync<TestRow>(Query());

        Assert.True(result.IsSuccess(out var success));
        Assert.Equal(new TestRow(1, "one"), success);
        Assert.Equal(1, TestRow.BindCount);
        Assert.Equal(1, connection.LastCommand!.LastReader!.ReadCount);
    }

    [Fact]
    public async Task ZeroRowWriteIsSuccessfulData()
    {
        var connection = new FakeDbConnection { NonQueryResult = 0 };
        await using var writer = new DbWriter(connection: connection);

        var result = await writer.ExecuteAsync(Query());

        Assert.True(result.IsSuccess(out var success));
        Assert.Equal(0, success);
        Assert.True(connection.LastCommand!.IsDisposed);
    }

    [Fact]
    public async Task ProviderExceptionEscapesUnchangedAndResourcesAreDisposed()
    {
        var expected = new InvalidOperationException("provider fault");
        var connection = new FakeDbConnection { ExecuteException = expected };
        await using var reader = new DbReader(connection: connection);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ExistsAsync(Query()));

        Assert.Same(expected, actual);
        Assert.True(connection.LastCommand!.IsDisposed);
    }

    [Fact]
    public async Task CancellationEscapesAndCommandIsDisposed()
    {
        var connection = new FakeDbConnection();
        await using var reader = new DbReader(connection: connection);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ExistsAsync(Query(), cancellation.Token));

        Assert.Null(connection.LastCommand);
    }

    [Fact]
    public async Task CancellationAfterCommandCreationEscapesAndDisposesCommand()
    {
        var connection = new FakeDbConnection { ExecuteGate = new TaskCompletionSource() };
        await using var reader = new DbReader(connection: connection);
        using var cancellation = new CancellationTokenSource();
        var operation = reader.ExistsAsync(Query(), cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.True(connection.LastCommand!.IsDisposed);
    }

    [Fact]
    public async Task ActiveOperationRejectsOverlapAndDisposal()
    {
        var connection = new FakeDbConnection { ExecuteGate = new TaskCompletionSource() };
        var reader = new DbReader(connection: connection);
        var operation = reader.ExistsAsync(Query());

        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ExistsAsync(Query()));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await reader.DisposeAsync());

        connection.ExecuteGate.SetResult();
        await operation;
        await reader.DisposeAsync();
        await reader.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => reader.ExistsAsync(Query()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(typeof(DBNull))]
    public async Task ScalarDatabaseNullIsSuccessfulNull(Type? nullKind)
    {
        var connection = new FakeDbConnection
        {
            ScalarValue = nullKind is null ? null : DBNull.Value
        };
        await using var reader = new DbReader(connection: connection);

        var result = await reader.ScalarAsync<string>(Query());

        Assert.True(result.IsSuccess(out var value));
        Assert.Null(value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistsReturnsSuccessfulBoolean(bool hasRow)
    {
        var connection = new FakeDbConnection();
        if (hasRow)
        {
            connection.Rows.Add([1, "one"]);
        }
        await using var reader = new DbReader(connection: connection);

        var result = await reader.ExistsAsync(Query());

        Assert.True(result.IsSuccess(out var value));
        Assert.Equal(hasRow, value);
    }

    [Fact]
    public async Task ExistingConnectionUsesCommandParameterAndDatabaseNull()
    {
        var connection = new FakeDbConnection();
        await using var reader = new DbReader(connection: connection);
        var query = new TestQueryBuilder(new CompiledSql(
            "select",
            [new SqlParameterSpec("@value", null)]
        ));

        await reader.ExistsAsync(query);

        var parameter = Assert.IsType<FakeDbParameter>(connection.LastCommand!.Parameters[0]);
        Assert.Equal(DBNull.Value, parameter.Value);
    }

    [Fact]
    public async Task ProviderReturningNoConnectionThrowsSafeMiseFault()
    {
        var connection = new FakeDbConnection();
        var factory = new FakeDbProviderFactory(connection) { ReturnNullConnection = true };
        await using var reader = new DbReader(new TestConfig("secret", factory));

        var exception = await Assert.ThrowsAsync<InvalidMappingException>(() => reader.ExistsAsync(Query()));

        Assert.DoesNotContain("secret", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProviderReturningNoParameterThrowsSafeMiseFault()
    {
        var connection = new FakeDbConnection();
        var factory = new FakeDbProviderFactory(connection) { ReturnNullParameter = true };
        await using var reader = new DbReader(new TestConfig("secret", factory));

        var exception = await Assert.ThrowsAsync<InvalidMappingException>(() => reader.ExistsAsync(QueryWithParameter()));

        Assert.DoesNotContain("secret", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, connection.LastCommand!.DisposeCount);
    }

    [Fact]
    public async Task DisposalExceptionEscapesUnchanged()
    {
        var expected = new InvalidOperationException("dispose fault");
        var connection = new FakeDbConnection { CommandDisposeException = expected };
        await using var reader = new DbReader(connection: connection);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ExistsAsync(Query()));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void ConstructorRequiresExactlyOneConnectionSource()
    {
        var connection = new FakeDbConnection();
        var config = new TestConfig("credential", new FakeDbProviderFactory(connection));

        var neither = Assert.Throws<ArgumentException>(() => new DbReader());
        var both = Assert.Throws<ArgumentException>(() => new DbReader(config, connection));

        Assert.DoesNotContain("credential", neither.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("credential", both.Message, StringComparison.Ordinal);
    }

    private static TestQueryBuilder Query() => new(new CompiledSql("select"));

    private static TestQueryBuilder QueryWithParameter()
    {
        return new TestQueryBuilder(new CompiledSql(
            "select",
            [new SqlParameterSpec("@id", 7, DbType.Int32)],
            timeout: 19,
            behavior: CommandBehavior.SingleResult
        ));
    }
}
