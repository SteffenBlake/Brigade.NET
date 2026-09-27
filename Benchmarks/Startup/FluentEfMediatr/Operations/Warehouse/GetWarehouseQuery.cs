using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Warehouse;

public sealed record GetWarehouseQuery(long Id) : IRequest<ResourceResult>;
