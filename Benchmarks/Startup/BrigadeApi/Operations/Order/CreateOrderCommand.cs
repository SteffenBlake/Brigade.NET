using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Order;

[Expo]
public sealed partial class CreateOrderCommand : IExpoValidatable
{
    [FromPayload, IsRequired]
    public CreateOrderPayload? Body { get; init; }
}

[Expo]
public sealed partial class CreateOrderPayload
{
    [IsRequired, StringHasMinimumLength(3), StringHasMaximumLength(80)]
    public string? Name { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100)]
    public int Score { get; init; }
}
