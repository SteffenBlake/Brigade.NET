using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class CreateItemHandler<TContext>(TContext context)
    : IRequestHandler<CreateItemCommand<TContext>, CreateDecision>
    where TContext : BenchmarkDbContext
{
    public async Task<CreateDecision> Handle(
        CreateItemCommand<TContext> request,
        CancellationToken cancellationToken
    )
    {
        var input = request.Payload;
        var category = await context.Categories.AsNoTracking()
            .Where(value => value.Id == input.CategoryId)
            .Select(value => new { value.Active, value.MinScore })
            .SingleOrDefaultAsync(cancellationToken);
        if (category is null)
        {
            return new CreateDecision(null, "Category not found");
        }

        if (category.Active != 1 || input.Score < category.MinScore)
        {
            return new CreateDecision(null, "Category does not accept this item");
        }

        var item = new ItemEntity
        {
            Title = input.Title!,
            CategoryId = input.CategoryId,
            Score = input.Score
        };
        context.Items.Add(item);
        await context.SaveChangesAsync(cancellationToken);

        return new CreateDecision(item.Id, null);
    }
}
