using Brigade.Net.Benchmarks.Api.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed record CreateSqlServerContext(
    [Provide] IDbConfig Config,
    [Provide] DbWriter Writer
);

public sealed class CreateSqlServerHandler
    : ICommandHandler<CreateItemCommand, CreateResult, CreateSqlServerContext>
{
    public static async Task<Result<CreateResult>> RunAsync(
        UnitOfWork work,
        CreateSqlServerContext context,
        CreateItemCommand command,
        CancellationToken ct
    )
    {
        var body = command.Body!;
        var categoryQuery = new SqlServerQueryBuilder()
            .Select($"{CategoryTblSqlServer.IdCol:raw}")
            .Select($"{CategoryTblSqlServer.ActiveCol:raw}")
            .Select($"{CategoryTblSqlServer.MinScoreCol:raw}")
            .From($"{CategoryTblSqlServer.Table:raw}")
            .Where($"{CategoryTblSqlServer.IdCol:raw} = {body.CategoryId}");
        Result<CategoryRowSqlServer> found;
        await using (var reader = new DbReader(context.Config))
        {
            found = await reader.FirstOrNotFoundAsync<CategoryRowSqlServer>(categoryQuery, ct);
        }
        if (!found.IsSuccess(out var category))
        {
            return new Error(Title: "Category not found", Status: 422);
        }

        if (category.Active != 1 || body.Score < category.MinScore)
        {
            return new Error(Title: "Category does not accept this item", Status: 422);
        }

        var insert = new SqlServerCommandBuilder()
            .OutputInserted("id")
            .InsertInto($"{ItemTblSqlServer.Table:raw}")
            .Columns($"title, category_id, score")
            .Values($"{body.Title}, {body.CategoryId}, {body.Score}");
        var identity = await context.Writer.ExecuteScalarAsync<long>(insert, ct);
        return identity.Map(id => new CreateResult(
            id
        ));
    }
}
