using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record CreatePostgreSqlContext(
    [Provide] IDbConfig Config,
    [Provide] DbWriter Writer
);

public sealed class CreatePostgreSqlHandler
    : ICommandHandler<CreateItemCommand, CreateResult, CreatePostgreSqlContext>
{
    public static async Task<Result<CreateResult>> RunAsync(
        UnitOfWork work,
        CreatePostgreSqlContext context,
        CreateItemCommand command,
        CancellationToken ct
    )
    {
        var body = command.Body!;
        var categoryQuery = new PostgreSqlQueryBuilder()
            .Select($"{CategoryTblPostgreSql.IdCol:raw}")
            .Select($"{CategoryTblPostgreSql.ActiveCol:raw}")
            .Select($"{CategoryTblPostgreSql.MinScoreCol:raw}")
            .From($"{CategoryTblPostgreSql.Table:raw}")
            .Where($"{CategoryTblPostgreSql.IdCol:raw} = {body.CategoryId}");
        Result<CategoryRowPostgreSql> found;
        await using (var reader = new DbReader(context.Config))
        {
            found = await reader.FirstOrNotFoundAsync<CategoryRowPostgreSql>(categoryQuery, ct);
        }
        if (!found.IsSuccess(out var category))
        {
            return new Error(Title: "Category not found", Status: 422);
        }

        if (category.Active != 1 || body.Score < category.MinScore)
        {
            return new Error(Title: "Category does not accept this item", Status: 422);
        }

        var insert = new PostgreSqlCommandBuilder()
            .Returning("id")
            .InsertInto($"{ItemTblPostgreSql.Table:raw}")
            .Columns($"title, category_id, score")
            .Values($"{body.Title}, {body.CategoryId}, {body.Score}");
        var identity = await context.Writer.ExecuteScalarAsync<long>(insert, ct);
        return identity.Map(id => new CreateResult(
            id
        ));
    }
}
