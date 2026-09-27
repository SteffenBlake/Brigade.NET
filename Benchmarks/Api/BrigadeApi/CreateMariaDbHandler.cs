using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record CreateMariaDbContext(
    [Provide] IDbConfig Config,
    [Provide] DbWriter Writer
);

public sealed class CreateMariaDbHandler
    : ICommandHandler<CreateItemCommand, CreateResult, CreateMariaDbContext>
{
    public static async Task<Result<CreateResult>> RunAsync(
        UnitOfWork work,
        CreateMariaDbContext context,
        CreateItemCommand command,
        CancellationToken ct
    )
    {
        var body = command.Body!;
        var categoryQuery = new MariaDbQueryBuilder()
            .Select($"{CategoryTblMariaDb.IdCol:raw}")
            .Select($"{CategoryTblMariaDb.ActiveCol:raw}")
            .Select($"{CategoryTblMariaDb.MinScoreCol:raw}")
            .From($"{CategoryTblMariaDb.Table:raw}")
            .Where($"{CategoryTblMariaDb.IdCol:raw} = {body.CategoryId}");
        Result<CategoryRowMariaDb> found;
        await using (var reader = new DbReader(context.Config))
        {
            found = await reader.FirstOrNotFoundAsync<CategoryRowMariaDb>(categoryQuery, ct);
        }
        if (!found.IsSuccess(out var category))
        {
            return new Error(Title: "Category not found", Status: 422);
        }

        if (category.Active != 1 || body.Score < category.MinScore)
        {
            return new Error(Title: "Category does not accept this item", Status: 422);
        }

        var insert = new MariaDbCommandBuilder()
            .Returning("id")
            .InsertInto($"{ItemTblMariaDb.Table:raw}")
            .Columns($"title, category_id, score")
            .Values($"{body.Title}, {body.CategoryId}, {body.Score}");
        var identity = await context.Writer.ExecuteScalarAsync<long>(insert, ct);
        return identity.Map(id => new CreateResult(
            id
        ));
    }
}
