using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;

public sealed record CreateInvoiceCommand(string? Name, int Score) : IRequest<ResourceResult>;
