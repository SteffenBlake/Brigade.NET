using Brigade.Net.Partie;
using Brigade.Net.Partie.Engines.AspNetCore;
using Brigade.Net.Partie.Extensions.Expo;
using Brigade.Net.Partie.Extensions.Mise.SQLite;
using Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Customer;
using Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Product;
using Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Order;
using Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Invoice;
using Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Shipment;
using Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Warehouse;
using PartieSystemBundleAttribute = Brigade.Net.Partie.AspNetCore.PartieSystemBundleAttribute;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi;

[BrigadeGroup("/api")]
[PartieSystemBundle]
[ExpoSystemBundle]
public static partial class Routing
{
    [MiseSqliteBundle("Main")]
    [CreateCustomerHandlerRoute.Post("/customers")]
    static void CreateCustomer(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Write");
    }

    [MiseSqliteBundle("Main")]
    [GetCustomerHandlerRoute.Get("/customers/{id}")]
    static void GetCustomer(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Read");
    }

    [MiseSqliteBundle("Main")]
    [CreateProductHandlerRoute.Post("/products")]
    static void CreateProduct(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Write");
    }

    [MiseSqliteBundle("Main")]
    [GetProductHandlerRoute.Get("/products/{id}")]
    static void GetProduct(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Read");
    }

    [MiseSqliteBundle("Reporting")]
    [CreateOrderHandlerRoute.Post("/orders")]
    static void CreateOrder(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Write");
    }

    [MiseSqliteBundle("Reporting")]
    [GetOrderHandlerRoute.Get("/orders/{id}")]
    static void GetOrder(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Read");
    }

    [MiseSqliteBundle("Audit")]
    [CreateInvoiceHandlerRoute.Post("/invoices")]
    static void CreateInvoice(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Write");
    }

    [MiseSqliteBundle("Audit")]
    [GetInvoiceHandlerRoute.Get("/invoices/{id}")]
    static void GetInvoice(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Read");
    }

    [MiseSqliteBundle("Archive")]
    [CreateShipmentHandlerRoute.Post("/shipments")]
    static void CreateShipment(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Write");
    }

    [MiseSqliteBundle("Archive")]
    [GetShipmentHandlerRoute.Get("/shipments/{id}")]
    static void GetShipment(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Read");
    }

    [MiseSqliteBundle("Reporting")]
    [CreateWarehouseHandlerRoute.Post("/warehouses")]
    static void CreateWarehouse(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Write");
    }

    [MiseSqliteBundle("Reporting")]
    [GetWarehouseHandlerRoute.Get("/warehouses/{id}")]
    static void GetWarehouse(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("Read");
    }
}
