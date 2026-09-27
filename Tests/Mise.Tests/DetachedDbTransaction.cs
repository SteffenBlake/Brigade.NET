using System.Data;
using System.Data.Common;

namespace Brigade.Net.Mise.Tests;

internal sealed class DetachedDbTransaction : DbTransaction
{
    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    protected override DbConnection DbConnection => null!;

    public override void Commit()
    {
        throw new InvalidOperationException("A detached transaction cannot commit.");
    }

    public override void Rollback()
    {
        throw new InvalidOperationException("A detached transaction cannot roll back.");
    }
}
