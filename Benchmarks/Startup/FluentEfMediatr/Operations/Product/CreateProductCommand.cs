using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Product;

public sealed record CreateProductCommand(string? Name, int Score) : IRequest<ResourceResult>;
