using Brigade.Net.Benchmarks.Api.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed record SearchItemsQuery<TContext>(int CategoryId, int MinScore)
    : IRequest<IReadOnlyList<ItemResult>>
    where TContext : BenchmarkDbContext;
