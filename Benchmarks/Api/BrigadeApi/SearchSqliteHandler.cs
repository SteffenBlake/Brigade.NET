using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record SearchSqliteContext([Provide] DbReader Reader);

public sealed class SearchSqliteHandler
    : IQueryHandler<SearchItemsQuery, IReadOnlyList<ItemResult>, SearchSqliteContext>
{
    public static async Task<Result<IReadOnlyList<ItemResult>>> RunAsync(
        SearchSqliteContext context,
        SearchItemsQuery request,
        CancellationToken ct
    )
    {
        var query = new SqliteQueryBuilder()
            .Select($"{ItemTblSqlite.IdCol:raw}")
            .Select($"{ItemTblSqlite.TitleCol:raw}")
            .Select($"{ItemTblSqlite.CategoryIdCol:raw}")
            .Select($"{ItemTblSqlite.ScoreCol:raw}")
            .From($"{ItemTblSqlite.Table:raw}")
            .Where($"{ItemTblSqlite.CategoryIdCol:raw} = {request.CategoryId}")
            .Where($"{ItemTblSqlite.ScoreCol:raw} >= {request.MinScore}")
            .OrderBy($"{ItemTblSqlite.IdCol:raw}")
            .Limit(20);
        var rows = await context.Reader.ListAsync<ItemRowSqlite>(query, ct);

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
