using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record CreateSqliteContext(
    [Provide] IDbConfig Config,
    [Provide] DbWriter Writer
);

public sealed class CreateSqliteHandler
    : ICommandHandler<CreateItemCommand, CreateResult, CreateSqliteContext>
{
    public static async Task<Result<CreateResult>> RunAsync(
        UnitOfWork work,
        CreateSqliteContext context,
        CreateItemCommand command,
        CancellationToken ct
    )
    {
        var body = command.Body!;
        var categoryQuery = new SqliteQueryBuilder()
            .Select($"{CategoryTblSqlite.IdCol:raw}")
            .Select($"{CategoryTblSqlite.ActiveCol:raw}")
            .Select($"{CategoryTblSqlite.MinScoreCol:raw}")
            .From($"{CategoryTblSqlite.Table:raw}")
            .Where($"{CategoryTblSqlite.IdCol:raw} = {body.CategoryId}");
        Result<CategoryRowSqlite> found;
        await using (var reader = new DbReader(context.Config))
        {
            found = await reader.FirstOrNotFoundAsync<CategoryRowSqlite>(categoryQuery, ct);
        }
        if (!found.IsSuccess(out var category))
        {
            return new Error(Title: "Category not found", Status: 422);
        }

        if (category.Active != 1 || body.Score < category.MinScore)
        {
            return new Error(Title: "Category does not accept this item", Status: 422);
        }

        var insert = new SqliteCommandBuilder()
            .Returning("id")
            .InsertInto($"{ItemTblSqlite.Table:raw}")
            .Columns($"title, category_id, score")
            .Values($"{body.Title}, {body.CategoryId}, {body.Score}");
        var identity = await context.Writer.ExecuteScalarAsync<long>(insert, ct);
        return identity.Map(id => new CreateResult(
            id
        ));
    }
}
