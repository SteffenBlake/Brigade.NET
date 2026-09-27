using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record SearchMariaDbContext([Provide] DbReader Reader);

public sealed class SearchMariaDbHandler
    : IQueryHandler<SearchItemsQuery, IReadOnlyList<ItemResult>, SearchMariaDbContext>
{
    public static async Task<Result<IReadOnlyList<ItemResult>>> RunAsync(
        SearchMariaDbContext context,
        SearchItemsQuery request,
        CancellationToken ct
    )
    {
        var query = new MariaDbQueryBuilder()
            .Select($"{ItemTblMariaDb.IdCol:raw}")
            .Select($"{ItemTblMariaDb.TitleCol:raw}")
            .Select($"{ItemTblMariaDb.CategoryIdCol:raw}")
            .Select($"{ItemTblMariaDb.ScoreCol:raw}")
            .From($"{ItemTblMariaDb.Table:raw}")
            .Where($"{ItemTblMariaDb.CategoryIdCol:raw} = {request.CategoryId}")
            .Where($"{ItemTblMariaDb.ScoreCol:raw} >= {request.MinScore}")
            .OrderBy($"{ItemTblMariaDb.IdCol:raw}")
            .Limit(20);
        var rows = await context.Reader.ListAsync<ItemRowMariaDb>(query, ct);

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
