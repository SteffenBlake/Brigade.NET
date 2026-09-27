using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Customer;

[Expo]
public sealed partial class CreateCustomerCommand : IExpoValidatable
{
    [FromPayload, IsRequired]
    public CreateCustomerPayload? Body { get; init; }
}

[Expo]
public sealed partial class CreateCustomerPayload
{
    [IsRequired, StringHasMinimumLength(3), StringHasMaximumLength(80)]
    public string? Name { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100)]
    public int Score { get; init; }
}
