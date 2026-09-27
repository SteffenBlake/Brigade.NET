using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Warehouse;

[Expo]
public sealed partial class CreateWarehouseCommand : IExpoValidatable
{
    [FromPayload, IsRequired]
    public CreateWarehousePayload? Body { get; init; }
}

[Expo]
public sealed partial class CreateWarehousePayload
{
    [IsRequired, StringHasMinimumLength(3), StringHasMaximumLength(80)]
    public string? Name { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100)]
    public int Score { get; init; }
}
