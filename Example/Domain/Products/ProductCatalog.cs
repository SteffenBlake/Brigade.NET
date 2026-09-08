namespace Brigade.Net.Example.Domain.Products;

public static class ProductCatalog
{
    public static decimal? Price(string sku) => sku switch
    {
        "NOTEBOOK" => 12.50m,
        "PEN" => 2.25m,
        _ => null
    };
}