using System.Data.Common;
using Brigade.Net.Core.Results;

namespace Brigade.Net.Mise;

/// <summary>Executes write commands against one non-concurrent connection scope.</summary>
/// <remarks>
/// Configuration creates an owned connection. An existing connection remains caller-owned.
/// Commands are always asynchronously disposed, and provider exceptions escape unchanged.
/// </remarks>
/// <param name="config">Configuration for an owned connection.</param>
/// <param name="connection">An existing caller-owned connection.</param>
public class DbWriter(IMiseConfig? config = null, DbConnection? connection = null) : DbReader(config, connection)
{
    /// <summary>Executes a write and returns its affected-row count, including zero.</summary>
    public async Task<Result<int>> ExecuteAsync(
        IQueryBuilder query,
        CancellationToken cancellationToken = default
    )
    {
        Enter();
        try
        {
            await using var command = await CreateCommandAsync(query, cancellationToken);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            Exit();
        }
    }
}
