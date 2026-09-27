using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record SearchSqlServerContext([Provide] DbReader Reader);

public sealed class SearchSqlServerHandler
    : IQueryHandler<SearchItemsQuery, IReadOnlyList<ItemResult>, SearchSqlServerContext>
{
    public static async Task<Result<IReadOnlyList<ItemResult>>> RunAsync(
        SearchSqlServerContext context,
        SearchItemsQuery request,
        CancellationToken ct
    )
    {
        var query = new SqlServerQueryBuilder()
            .Select($"{ItemTblSqlServer.IdCol:raw}")
            .Select($"{ItemTblSqlServer.TitleCol:raw}")
            .Select($"{ItemTblSqlServer.CategoryIdCol:raw}")
            .Select($"{ItemTblSqlServer.ScoreCol:raw}")
            .From($"{ItemTblSqlServer.Table:raw}")
            .Where($"{ItemTblSqlServer.CategoryIdCol:raw} = {request.CategoryId}")
            .Where($"{ItemTblSqlServer.ScoreCol:raw} >= {request.MinScore}")
            .OrderBy($"{ItemTblSqlServer.IdCol:raw}")
            .Limit(20);
        var rows = await context.Reader.ListAsync<ItemRowSqlServer>(query, ct);

        return rows.Map<IReadOnlyList<ItemResult>>(items =>
            items.Select(item => new ItemResult(
                item.Id,
                item.Title,
                item.CategoryId,
                item.Score
            )).ToArray()
        );
    }
}
