using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Order;

public sealed record CreateOrderCommand(string? Name, int Score) : IRequest<ResourceResult>;
