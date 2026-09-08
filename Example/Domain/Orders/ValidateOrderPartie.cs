using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public static class ValidateOrderPartie
{
    public static ValueTask<Result<TResult>> InvokeAsync<TResult>(
        [FromBody] PlaceOrder request,
        OrderRequestScope scope,
        Next<Unit, TResult> next
    )
    {
        scope.Events.Add("validate");
        if (string.IsNullOrWhiteSpace(request.Customer) || request.Customer.Length > 100
            || request.Quantity is < 1 or > 100 || ProductCatalog.Price(request.Sku) is null
        )
        {
            return ValueTask.FromResult<Result<TResult>>(
                new Error(
                    Title: "Invalid order",
                    Detail: "Provide a customer, a known SKU, and a quantity between 1 and 100."
                )
            );
        }

        return next(Unit.Default);
    }
}