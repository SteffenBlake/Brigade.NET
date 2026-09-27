using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;

public sealed record GetShipmentQuery(long Id) : IRequest<ResourceResult>;
