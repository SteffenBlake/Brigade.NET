using System.Data;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

public sealed class ExecutionContractTests
{
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
        var query = new TestQueryBuilder(new MiseCommand(
            "select",
            [new MiseParameter("@value", null)]
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

        var exception = await Assert.ThrowsAsync<MiseInvalidMappingException>(() => reader.ExistsAsync(Query()));

        Assert.DoesNotContain("secret", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProviderReturningNoParameterThrowsSafeMiseFault()
    {
        var connection = new FakeDbConnection();
        var factory = new FakeDbProviderFactory(connection) { ReturnNullParameter = true };
        await using var reader = new DbReader(new TestConfig("secret", factory));

        var exception = await Assert.ThrowsAsync<MiseInvalidMappingException>(() => reader.ExistsAsync(QueryWithParameter()));

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

    private static TestQueryBuilder Query() => new(new MiseCommand("select"));

    private static TestQueryBuilder QueryWithParameter()
    {
        return new TestQueryBuilder(new MiseCommand(
            "select",
            [new MiseParameter("@id", 7, DbType.Int32)],
            timeout: 19
        ));
    }
}
