using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;

public static class ComplexQueryFactory
{
    public static QueryBuilder CreateMiseQuery()
    {
        const decimal minAmount = 125m;
        const int minQuantity = 3;
        const int minCount = 2;

        var qualifyingLines = new SqlServerQueryBuilder()
            .Select($"1")
            .From($"purchase_lines AS l")
            .Where($"l.purchase_id = p.id")
            .Where($"l.quantity >= {minQuantity}");

        return new SqlServerQueryBuilder()
            .Select($"a.region AS Region")
            .Select($"p.category AS Category")
            .Select($"COUNT(*) AS PurchaseCount")
            .Select($"SUM(p.amount) AS TotalAmount")
            .Select($"MAX(s.shipped_at) AS LastShippedAt")
            .From($"purchases AS p")
            .InnerJoin($"accounts AS a ON p.account_id = a.id")
            .LeftJoin($"shipments AS s ON p.id = s.purchase_id")
            .Where($"a.active = {true}")
            .Where($"p.amount >= {minAmount}")
            .WhereExists(qualifyingLines)
            .Where($"(s.id IS NULL OR s.delivered = {true})")
            .GroupBy($"a.region")
            .GroupBy($"p.category")
            .Having($"COUNT(*) >= {minCount}")
            .OrderBy($"SUM(p.amount) DESC")
            .OrderBy($"a.region ASC")
            .Offset(10)
            .Limit(20);
    }

    public static IQueryable<ComplexProjection> CreateEfQuery(CompilationDbContext context)
    {
        const decimal minAmount = 125m;
        const int minQuantity = 3;
        const int minCount = 2;

        var query =
            from purchase in context.Purchases
            join account in context.Accounts on purchase.AccountId equals account.Id
            join shipment in context.Shipments on purchase.Id equals shipment.PurchaseId into shipmentGroup
            from shipment in shipmentGroup.DefaultIfEmpty()
            where account.Active
                && purchase.Amount >= minAmount
                && context.Lines.Any(line => line.PurchaseId == purchase.Id && line.Quantity >= minQuantity)
                && (shipment == null || shipment.Delivered)
            group new { purchase, shipment } by new { account.Region, purchase.Category } into grouped
            where grouped.Count() >= minCount
            orderby grouped.Sum(row => row.purchase.Amount) descending, grouped.Key.Region
            select new ComplexProjection
            {
                Region = grouped.Key.Region,
                Category = grouped.Key.Category,
                PurchaseCount = grouped.Count(),
                TotalAmount = grouped.Sum(row => row.purchase.Amount),
                LastShippedAt = grouped.Max(row => row.shipment == null ? null : row.shipment.ShippedAt)
            };

        return query.Skip(10).Take(20);
    }
}
