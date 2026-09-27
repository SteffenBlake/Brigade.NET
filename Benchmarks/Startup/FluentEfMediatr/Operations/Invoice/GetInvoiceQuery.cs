using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;

public sealed record GetInvoiceQuery(long Id) : IRequest<ResourceResult>;
