using Brigade.Net.Mise;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Common;

public static class PointLookupQueries
{
    public static async Task<AccountRow> MiseAsync(
        DbReader reader,
        string database,
        int id
    )
    {
        var query = DatabasePlatform.CreateQuery(database, id);
        var result = await reader.FirstOrNotFoundAsync<AccountRow>(query);
        if (!result.IsSuccess(out var row))
        {
            throw new InvalidOperationException($"Account {id} was not found in {database}.");
        }

        return row;
    }

    public static Task<AccountRow> DapperAsync(DbConnection connection, int id)
    {
        return connection.QueryFirstAsync<AccountRow>(
            "SELECT id, name FROM benchmark_accounts WHERE id = @id",
            new { id }
        );
    }

    public static Task<AccountRow> EfCoreAsync(BenchmarkDbContext context, int id)
    {
        return context.Accounts.AsNoTracking().FirstAsync(row => row.Id == id);
    }
}
