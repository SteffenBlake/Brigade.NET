using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record SearchMySqlContext([Provide] DbReader Reader);

public sealed class SearchMySqlHandler
    : IQueryHandler<SearchItemsQuery, IReadOnlyList<ItemResult>, SearchMySqlContext>
{
    public static async Task<Result<IReadOnlyList<ItemResult>>> RunAsync(
        SearchMySqlContext context,
        SearchItemsQuery request,
        CancellationToken ct
    )
    {
        var query = new MySqlQueryBuilder()
            .Select($"{ItemTblMySql.IdCol:raw}")
            .Select($"{ItemTblMySql.TitleCol:raw}")
            .Select($"{ItemTblMySql.CategoryIdCol:raw}")
            .Select($"{ItemTblMySql.ScoreCol:raw}")
            .From($"{ItemTblMySql.Table:raw}")
            .Where($"{ItemTblMySql.CategoryIdCol:raw} = {request.CategoryId}")
            .Where($"{ItemTblMySql.ScoreCol:raw} >= {request.MinScore}")
            .OrderBy($"{ItemTblMySql.IdCol:raw}")
            .Limit(20);
        var rows = await context.Reader.ListAsync<ItemRowMySql>(query, ct);

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
