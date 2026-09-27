using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;

public sealed record CreateShipmentCommand(string? Name, int Score) : IRequest<ResourceResult>;
