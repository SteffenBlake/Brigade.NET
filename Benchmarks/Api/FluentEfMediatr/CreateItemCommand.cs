using Brigade.Net.Benchmarks.Api.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed record CreateItemCommand<TContext>(CreateItemPayload Payload)
    : IRequest<CreateDecision>
    where TContext : BenchmarkDbContext;
