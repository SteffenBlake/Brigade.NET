using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[Expo]
public sealed partial class CreateItemCommand : IExpoValidatable
{
    [FromPayload]
    [IsRequired]
    public CreateItemPayload? Body { get; init; }
}

[Expo]
public sealed partial class CreateItemPayload
{
    [IsRequired, StringHasMinimumLength(3), StringHasMaximumLength(80)]
    public string? Title { get; init; }

    [IsGreaterThanOrEqualTo(1), IsLessThanOrEqualTo(10)]
    public int CategoryId { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100)]
    public int Score { get; init; }
}
