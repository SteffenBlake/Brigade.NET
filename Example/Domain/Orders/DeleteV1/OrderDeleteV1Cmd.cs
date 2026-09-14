using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.DeleteV1;
/// <summary>Delete an order.</summary>
public sealed class OrderDeleteV1Cmd
{
    /// <summary>The order ID.</summary>
    [FromPath(Name = "id")]
    public required Guid Id { get; init; }
}
