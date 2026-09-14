using Brigade.Net.Core.Results;

namespace Brigade.Net.Example.Domain.Orders;

public interface IOrderStore
{
    Order Create(
        string customer,
        string sku,
        int quantity,
        decimal unitPrice
    );
    Order[] Search(Guid? id, string? customer);
    Result<Unit> Delete(Guid id);
}
