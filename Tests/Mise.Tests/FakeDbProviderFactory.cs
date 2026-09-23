using System.Data.Common;

namespace Brigade.Net.Mise.Tests;

internal sealed class FakeDbProviderFactory(FakeDbConnection connection) : DbProviderFactory
{
    public FakeDbConnection Connection { get; } = connection;

    public int ParameterCreateCount { get; private set; }

    public bool ReturnNullConnection { get; set; }

    public bool ReturnNullParameter { get; set; }

    public override DbConnection CreateConnection() => ReturnNullConnection ? null! : Connection;

    public override DbParameter CreateParameter()
    {
        ParameterCreateCount++;
        return ReturnNullParameter ? null! : new FakeDbParameter();
    }
}
