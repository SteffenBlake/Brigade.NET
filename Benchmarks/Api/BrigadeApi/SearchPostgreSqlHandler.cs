using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record SearchPostgreSqlContext([Provide] DbReader Reader);

public sealed class SearchPostgreSqlHandler
    : IQueryHandler<SearchItemsQuery, IReadOnlyList<ItemResult>, SearchPostgreSqlContext>
{
    public static async Task<Result<IReadOnlyList<ItemResult>>> RunAsync(
        SearchPostgreSqlContext context,
        SearchItemsQuery request,
        CancellationToken ct
    )
    {
        var query = new PostgreSqlQueryBuilder()
            .Select($"{ItemTblPostgreSql.IdCol:raw}")
            .Select($"{ItemTblPostgreSql.TitleCol:raw}")
            .Select($"{ItemTblPostgreSql.CategoryIdCol:raw}")
            .Select($"{ItemTblPostgreSql.ScoreCol:raw}")
            .From($"{ItemTblPostgreSql.Table:raw}")
            .Where($"{ItemTblPostgreSql.CategoryIdCol:raw} = {request.CategoryId}")
            .Where($"{ItemTblPostgreSql.ScoreCol:raw} >= {request.MinScore}")
            .OrderBy($"{ItemTblPostgreSql.IdCol:raw}")
            .Limit(20);
        var rows = await context.Reader.ListAsync<ItemRowPostgreSql>(query, ct);

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
