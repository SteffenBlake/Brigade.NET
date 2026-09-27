using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record CreateMySqlContext(
    [Provide] IDbConfig Config,
    [Provide] DbWriter Writer
);

public sealed class CreateMySqlHandler
    : ICommandHandler<CreateItemCommand, CreateResult, CreateMySqlContext>
{
    public static async Task<Result<CreateResult>> RunAsync(
        UnitOfWork work,
        CreateMySqlContext context,
        CreateItemCommand command,
        CancellationToken ct
    )
    {
        var body = command.Body!;
        var categoryQuery = new MySqlQueryBuilder()
            .Select($"{CategoryTblMySql.IdCol:raw}")
            .Select($"{CategoryTblMySql.ActiveCol:raw}")
            .Select($"{CategoryTblMySql.MinScoreCol:raw}")
            .From($"{CategoryTblMySql.Table:raw}")
            .Where($"{CategoryTblMySql.IdCol:raw} = {body.CategoryId}");
        Result<CategoryRowMySql> found;
        await using (var reader = new DbReader(context.Config))
        {
            found = await reader.FirstOrNotFoundAsync<CategoryRowMySql>(categoryQuery, ct);
        }
        if (!found.IsSuccess(out var category))
        {
            return new Error(Title: "Category not found", Status: 422);
        }

        if (category.Active != 1 || body.Score < category.MinScore)
        {
            return new Error(Title: "Category does not accept this item", Status: 422);
        }

        var insert = new MySqlCommandBuilder()
            .InsertInto($"{ItemTblMySql.Table:raw}")
            .Columns($"title, category_id, score")
            .Values($"{body.Title}, {body.CategoryId}, {body.Score}");
        await context.Writer.ExecuteAsync(insert, ct);
        var identity = await context.Writer.ExecuteScalarAsync<long>(
            new MySqlCommandBuilder().Sql($"SELECT CAST(LAST_INSERT_ID() AS SIGNED)"),
            ct
        );
        return identity.Map(id => new CreateResult(
            id
        ));
    }
}
