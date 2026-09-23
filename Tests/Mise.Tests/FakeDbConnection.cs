using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Brigade.Net.Mise.Tests;

internal sealed class FakeDbConnection : DbConnection
{
    private ConnectionState _state;

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "fake";

    public override string DataSource => "fake";

    public override string ServerVersion => "1";

    public override ConnectionState State => _state;

    public int OpenCount { get; private set; }

    public int CloseCount { get; private set; }

    public int DisposeCount { get; private set; }

    public List<object?[]> Rows { get; } = [];

    public object? ScalarValue { get; set; }

    public int NonQueryResult { get; set; }

    public Exception? ExecuteException { get; set; }

    public TaskCompletionSource? ExecuteGate { get; set; }

    public Exception? CommandDisposeException { get; set; }

    public FakeDbCommand? LastCommand { get; private set; }

    public override void Open()
    {
        OpenCount++;
        _state = ConnectionState.Open;
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Open();
        return Task.CompletedTask;
    }

    public override void Close()
    {
        CloseCount++;
        _state = ConnectionState.Closed;
    }

    public override void ChangeDatabase(string databaseName)
    {
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw new NotSupportedException();
    }

    protected override DbCommand CreateDbCommand()
    {
        LastCommand = new FakeDbCommand(this);
        return LastCommand;
    }

    protected override void Dispose(bool disposing)
    {
        DisposeCount++;
        _state = ConnectionState.Closed;
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        Dispose(true);
        return ValueTask.CompletedTask;
    }
}
