using System.Data.Common;

namespace Brigade.Net.Mise;

/// <summary>Provides generated, reflection-free materialization for a result row.</summary>
/// <typeparam name="TSelf">The materialized row type.</typeparam>
public interface IMiseRow<TSelf>
    where TSelf : IMiseRow<TSelf>
{
    /// <summary>Binds projected column names to ordinals once for a result set.</summary>
    static abstract int[] BindOrdinals(DbDataReader reader);

    /// <summary>Materializes one row using ordinals previously bound for the result set.</summary>
    static abstract TSelf Materialize(DbDataReader reader, ReadOnlySpan<int> ordinals);
}
