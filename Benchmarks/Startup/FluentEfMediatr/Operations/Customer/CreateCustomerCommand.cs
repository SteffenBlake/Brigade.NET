using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Customer;

public sealed record CreateCustomerCommand(string? Name, int Score) : IRequest<ResourceResult>;
