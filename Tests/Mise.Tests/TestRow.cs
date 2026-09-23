using System.Data.Common;
using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

internal sealed record TestRow(int Id, string? Name) : IMiseRow<TestRow>
{
    public static int BindCount { get; set; }

    public static int[] BindOrdinals(DbDataReader reader)
    {
        BindCount++;
        return [reader.GetOrdinal("id"), reader.GetOrdinal("name")];
    }

    public static TestRow Materialize(DbDataReader reader, ReadOnlySpan<int> ordinals)
    {
        return new TestRow(
            reader.GetInt32(ordinals[0]),
            reader.IsDBNull(ordinals[1]) ? null : reader.GetString(ordinals[1])
        );
    }
}
