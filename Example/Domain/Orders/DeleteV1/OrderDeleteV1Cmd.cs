using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.DeleteV1;
/// <summary>Delete an order.</summary>
[Expo]
public sealed partial class OrderDeleteV1Cmd
{
    /// <summary>The order ID.</summary>
    [FromPath(Name = "orderId")]
    [CustomValidation]
    public required Guid Id { get; init; }

    private partial IEnumerable<string> ValidateId() => [];
}
