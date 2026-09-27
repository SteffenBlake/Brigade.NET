using Brigade.Net.Benchmarks.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class SearchItemsHandler<TContext>(TContext context)
    : IRequestHandler<SearchItemsQuery<TContext>, IReadOnlyList<ItemResult>>
    where TContext : BenchmarkDbContext
{
    public async Task<IReadOnlyList<ItemResult>> Handle(
        SearchItemsQuery<TContext> request,
        CancellationToken cancellationToken
    )
    {
        return await context.Items.AsNoTracking()
            .Where(item => item.CategoryId == request.CategoryId && item.Score >= request.MinScore)
            .OrderBy(item => item.Id)
            .Take(20)
            .Select(item => new ItemResult(item.Id, item.Title, item.CategoryId, item.Score))
            .ToArrayAsync(cancellationToken);
    }
}
