using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Customer;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Order;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Product;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Warehouse;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr;

public static class ApiRoutes
{
    public static void Map(WebApplication app)
    {
        CustomerRoutes.Map(app);
        ProductRoutes.Map(app);
        OrderRoutes.Map(app);
        InvoiceRoutes.Map(app);
        ShipmentRoutes.Map(app);
        WarehouseRoutes.Map(app);
    }
}
