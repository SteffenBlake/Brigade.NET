using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Brigade.Net.Mise.Tests;

internal sealed class FakeDbCommand(FakeDbConnection connection) : DbCommand
{
    private readonly FakeDbParameterCollection _parameters = new();

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; }

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    [AllowNull]
    protected override DbConnection DbConnection { get; set; } = connection;

    protected override DbParameterCollection DbParameterCollection => _parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public bool IsDisposed { get; private set; }

    public int DisposeCount { get; private set; }

    public DbTransaction? AttachedTransaction => DbTransaction;

    public CountingDbDataReader? LastReader { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public CommandBehavior LastBehavior { get; private set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        ThrowIfSet();
        return connection.NonQueryResult;
    }

    public override object? ExecuteScalar()
    {
        ThrowIfSet();
        return connection.ScalarValue;
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        LastBehavior = behavior;
        ThrowIfSet();
        LastReader = new CountingDbDataReader(CreateReader(), connection.LifecycleEvents.Add);
        return LastReader;
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken
    )
    {
        LastCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        if (connection.ExecuteGate is not null)
        {
            await connection.ExecuteGate.Task.WaitAsync(cancellationToken);
        }
        return ExecuteDbDataReader(behavior);
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ExecuteNonQuery());
    }

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ExecuteScalar());
    }

    protected override void Dispose(bool disposing)
    {
        connection.LifecycleEvents.Add("command.dispose");
        IsDisposed = true;
        DisposeCount++;
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        Dispose(true);
        if (connection.CommandDisposeException is not null)
        {
            return ValueTask.FromException(connection.CommandDisposeException);
        }
        return ValueTask.CompletedTask;
    }

    private DbDataReader CreateReader()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        foreach (var row in connection.Rows)
        {
            table.Rows.Add(row.Select(value => value ?? DBNull.Value).ToArray());
        }
        return table.CreateDataReader();
    }

    private void ThrowIfSet()
    {
        if (connection.ExecuteException is not null)
        {
            throw connection.ExecuteException;
        }
    }
}
