using System.Data.Common;
using Brigade.Net.Mise;

namespace Brigade.Net.Mise.IntegrationTests;

internal sealed record IntegrationRow(int Id, string? Name) : IRow<IntegrationRow>
{
    public static int[] BindOrdinals(DbDataReader reader)
    {
        return [reader.GetOrdinal("id"), reader.GetOrdinal("name")];
    }

    public static IntegrationRow Materialize(DbDataReader reader, ReadOnlySpan<int> ordinals)
    {
        return new IntegrationRow(
            reader.GetInt32(ordinals[0]),
            reader.IsDBNull(ordinals[1]) ? null : reader.GetString(ordinals[1])
        );
    }
}
