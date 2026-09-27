using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Product;

[Expo]
public sealed partial class CreateProductCommand : IExpoValidatable
{
    [FromPayload, IsRequired]
    public CreateProductPayload? Body { get; init; }
}

[Expo]
public sealed partial class CreateProductPayload
{
    [IsRequired, StringHasMinimumLength(3), StringHasMaximumLength(80)]
    public string? Name { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100)]
    public int Score { get; init; }
}
