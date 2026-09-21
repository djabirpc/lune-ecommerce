using Ecommerce.Application.Dashboard;
using Ecommerce.Application.Dashboard.Dtos;
using Ecommerce.Domain.Orders;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Dashboard;

/// <summary>
/// Backs the admin "Vue Générale" dashboard (GET /api/dashboard/overview). All numbers come from real
/// Orders/OrderStatusHistories/OrderItems/Inventory/Shipments data — nothing here is mocked.
///
/// Business-date decisions (CLAUDE.md section 21 / dashboard task spec — documented per its explicit
/// requirement, also mirrored in PROJECT_CONTEXT.md):
///   - Revenue (CA)     -> orders' DELIVERED date. COD money isn't real until the carrier actually
///                         collects it (CLAUDE.md section 40), so revenue is never counted on creation.
///   - Orders (count)   -> orders' CREATED date.
///   - Confirmed/Delivered/Returned/Cancelled KPIs -> the date each order first reached that status.
///   - Orders-by-status -> orders CREATED in the period, grouped by their CURRENT status (so a chart of
///     "last week's orders" reflects where those specific orders stand today, not a snapshot frozen at
///     creation time).
///   - Funnel           -> a "period-activity" funnel: each stage counts orders that reached that stage
///     at any point during the period (NOT a cohort tracked across the period boundary). This avoids
///     fabricating false precision a real cohort funnel would need weeks of headroom to compute honestly.
///   - Top products      -> OrderItems whose parent Order was CREATED in the period (sales velocity, not
///     lagged by fulfillment time).
///   - Delivery/returns  -> the order's own DELIVERED/RETURNED/REFUSED/DeliveryFailed date, attributed to
///     the linked Shipment's Carrier for the by-carrier breakdown.
///   - Marketing         -> orders CREATED in the period, grouped by the UtmSource captured at checkout.
///     No ROAS/ad-spend is computed — this data does not exist anywhere in the system.
///   - "À faire" queue + low-stock alerts -> current global state, NEVER period-filtered (a stock count
///     or an unconfirmed order backlog isn't a historical metric).
///
/// Every status-transition date is derived from OrderStatusHistories (Order has no dedicated
/// ConfirmedAtUtc/DeliveredAtUtc columns) via a single GROUP BY (OrderId, NewStatus) MIN(CreatedAtUtc)
/// query, split client-side into per-status lookup dictionaries — one round trip instead of one query
/// per status. Per the established EF Core gotcha in this codebase, GroupBy aggregates are materialized
/// into an anonymous type first (never straight into a record constructor) before being reshaped.
/// </summary>
public class DashboardService(AppDbContext dbContext) : IDashboardService
{
    private const int LowStockThreshold = 5;

    private record OrderSnapshot(Guid Id, DateTime CreatedAtUtc, decimal Total, OrderStatus Status, string? UtmSource);

    public async Task<DashboardOverviewDto> GetOverviewAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            (from, to) = (to, from);
        }

        var duration = to - from;
        var comparisonTo = from.AddTicks(-1);
        var comparisonFrom = comparisonTo - duration;

        var orderRows = await dbContext.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.CreatedAtUtc, o.Total, o.Status, o.UtmSource })
            .ToListAsync(cancellationToken);
        var orders = orderRows.Select(o => new OrderSnapshot(o.Id, o.CreatedAtUtc, o.Total, o.Status, o.UtmSource)).ToList();

        var reachedRaw = await dbContext.OrderStatusHistories.AsNoTracking()
            .GroupBy(h => new { h.OrderId, h.NewStatus })
            .Select(g => new { g.Key.OrderId, g.Key.NewStatus, ReachedAt = g.Min(h => h.CreatedAtUtc) })
            .ToListAsync(cancellationToken);
        var reachedByStatus = reachedRaw
            .GroupBy(r => r.NewStatus)
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.OrderId, r => r.ReachedAt));

        Dictionary<Guid, DateTime> ReachedAt(OrderStatus status) =>
            reachedByStatus.TryGetValue(status, out var dict) ? dict : [];

        var confirmedAt = ReachedAt(OrderStatus.Confirmed);
        var readyToShipAt = ReachedAt(OrderStatus.ReadyToShip);
        var shippedAt = ReachedAt(OrderStatus.Shipped);
        var deliveredAt = ReachedAt(OrderStatus.Delivered);
        var returnedAt = ReachedAt(OrderStatus.Returned);
        var cancelledAt = ReachedAt(OrderStatus.Cancelled);
        var outForDeliveryAt = ReachedAt(OrderStatus.OutForDelivery);
        var refusedAt = ReachedAt(OrderStatus.Refused);
        var deliveryFailedAt = ReachedAt(OrderStatus.DeliveryFailed);

        var hasComparisonData = orders.Count > 0 && orders.Min(o => o.CreatedAtUtc) <= comparisonTo;

        var kpis = new DashboardKpisDto(
            BuildKpi(SumByDate(orders, deliveredAt, from, to), SumByDate(orders, deliveredAt, comparisonFrom, comparisonTo), hasComparisonData),
            BuildKpi(orders.Count(o => InPeriod(o.CreatedAtUtc, from, to)), orders.Count(o => InPeriod(o.CreatedAtUtc, comparisonFrom, comparisonTo)), hasComparisonData),
            BuildKpi(CountByDate(confirmedAt, from, to), CountByDate(confirmedAt, comparisonFrom, comparisonTo), hasComparisonData),
            BuildKpi(CountByDate(deliveredAt, from, to), CountByDate(deliveredAt, comparisonFrom, comparisonTo), hasComparisonData),
            BuildKpi(CountByDate(returnedAt, from, to), CountByDate(returnedAt, comparisonFrom, comparisonTo), hasComparisonData),
            BuildKpi(CountByDate(cancelledAt, from, to), CountByDate(cancelledAt, comparisonFrom, comparisonTo), hasComparisonData));

        var (revenuePoints, granularity) = BuildRevenueSeries(orders, deliveredAt, from, to);

        var ordersByStatus = orders
            .Where(o => InPeriod(o.CreatedAtUtc, from, to))
            .GroupBy(o => o.Status)
            .Select(g => new DashboardStatusCountDto(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

        var funnel = BuildFunnel(orders, from, to, confirmedAt, readyToShipAt, shippedAt, deliveredAt);

        var topProducts = await BuildTopProductsAsync(from, to, cancellationToken);

        var delivery = await BuildDeliveryAsync(from, to, deliveredAt, outForDeliveryAt, refusedAt, returnedAt, deliveryFailedAt, cancellationToken);

        var returns = await BuildReturnsAsync(orders, returnedAt, from, to, cancellationToken);

        var marketing = orders
            .Where(o => InPeriod(o.CreatedAtUtc, from, to))
            .GroupBy(o => string.IsNullOrWhiteSpace(o.UtmSource) ? "Direct / Inconnu" : o.UtmSource!)
            .Select(g => new DashboardMarketingSourceStatDto(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(g => g.Revenue)
            .ToList();

        var actionQueue = BuildActionQueue(orders);

        var lowStock = await dbContext.Inventory.AsNoTracking()
            .Where(i => i.AvailableQuantity <= LowStockThreshold && i.ProductVariant.IsActive && i.ProductVariant.Product.IsActive)
            .OrderBy(i => i.AvailableQuantity)
            .Select(i => new DashboardStockAlertDto(
                i.ProductVariantId, i.ProductVariant.ProductId, i.ProductVariant.Product.Name, i.ProductVariant.Color, i.ProductVariant.Size, i.AvailableQuantity))
            .Take(15)
            .ToListAsync(cancellationToken);

        return new DashboardOverviewDto(
            new DashboardPeriodDto(from, to),
            new DashboardPeriodDto(comparisonFrom, comparisonTo),
            hasComparisonData,
            kpis,
            revenuePoints,
            granularity,
            ordersByStatus,
            funnel,
            topProducts,
            delivery,
            returns,
            marketing,
            actionQueue,
            lowStock);
    }

    private static bool InPeriod(DateTime date, DateTime from, DateTime to) => date >= from && date <= to;

    private static int CountByDate(Dictionary<Guid, DateTime> reachedAt, DateTime from, DateTime to) =>
        reachedAt.Values.Count(d => InPeriod(d, from, to));

    private static decimal SumByDate(List<OrderSnapshot> orders, Dictionary<Guid, DateTime> reachedAt, DateTime from, DateTime to) =>
        orders.Where(o => reachedAt.TryGetValue(o.Id, out var d) && InPeriod(d, from, to)).Sum(o => o.Total);

    private static DashboardKpiDto BuildKpi(decimal current, decimal previous, bool hasComparisonData)
    {
        if (!hasComparisonData)
        {
            return new DashboardKpiDto(current, null, null);
        }

        decimal? changePercent = previous == 0 ? null : Math.Round((current - previous) / previous * 100, 1);
        return new DashboardKpiDto(current, previous, changePercent);
    }

    private static (IReadOnlyList<DashboardRevenuePointDto> Points, string Granularity) BuildRevenueSeries(
        List<OrderSnapshot> orders, Dictionary<Guid, DateTime> deliveredAt, DateTime from, DateTime to)
    {
        var duration = to - from;
        string granularity;
        Func<DateTime, DateTime> bucketOf;
        Func<DateTime, DateTime> nextBucket;

        if (duration <= TimeSpan.FromDays(2))
        {
            granularity = "hourly";
            bucketOf = d => new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0, DateTimeKind.Utc);
            nextBucket = d => d.AddHours(1);
        }
        else if (duration <= TimeSpan.FromDays(93))
        {
            granularity = "daily";
            bucketOf = d => d.Date;
            nextBucket = d => d.AddDays(1);
        }
        else
        {
            granularity = "monthly";
            bucketOf = d => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            nextBucket = d => d.AddMonths(1);
        }

        var buckets = new SortedDictionary<DateTime, decimal>();
        for (var bucket = bucketOf(from); bucket <= to; bucket = nextBucket(bucket))
        {
            buckets[bucket] = 0m;
        }

        foreach (var order in orders)
        {
            if (!deliveredAt.TryGetValue(order.Id, out var deliveredDate) || !InPeriod(deliveredDate, from, to))
            {
                continue;
            }

            var bucket = bucketOf(deliveredDate);
            buckets[bucket] = buckets.GetValueOrDefault(bucket) + order.Total;
        }

        var points = buckets.Select(kv => new DashboardRevenuePointDto(kv.Key, kv.Value)).ToList();
        return (points, granularity);
    }

    private static IReadOnlyList<DashboardFunnelStageDto> BuildFunnel(
        List<OrderSnapshot> orders, DateTime from, DateTime to,
        Dictionary<Guid, DateTime> confirmedAt, Dictionary<Guid, DateTime> readyToShipAt,
        Dictionary<Guid, DateTime> shippedAt, Dictionary<Guid, DateTime> deliveredAt)
    {
        var stages = new (string Key, string Label, int Count)[]
        {
            ("created", "Commandes créées", orders.Count(o => InPeriod(o.CreatedAtUtc, from, to))),
            ("confirmed", "Confirmées", CountByDate(confirmedAt, from, to)),
            ("readyToShip", "Prêtes à expédier", CountByDate(readyToShipAt, from, to)),
            ("shipped", "Expédiées", CountByDate(shippedAt, from, to)),
            ("delivered", "Livrées", CountByDate(deliveredAt, from, to)),
        };

        var firstCount = stages[0].Count;
        return stages
            .Select((stage, index) => new DashboardFunnelStageDto(
                stage.Key,
                stage.Label,
                stage.Count,
                index == 0 || firstCount == 0 ? null : Math.Round((decimal)stage.Count / firstCount * 100, 1)))
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardTopProductDto>> BuildTopProductsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var raw = await dbContext.OrderItems.AsNoTracking()
            .Where(i => i.Order.CreatedAtUtc >= from && i.Order.CreatedAtUtc <= to)
            .Join(dbContext.ProductVariants.AsNoTracking(), i => i.ProductVariantId, v => v.Id, (i, v) => new { i.OrderId, i.Quantity, i.LineTotal, v.ProductId })
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Units = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal),
                OrderCount = g.Select(x => x.OrderId).Distinct().Count(),
            })
            .OrderByDescending(x => x.Units)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (raw.Count == 0)
        {
            return [];
        }

        var productIds = raw.Select(r => r.ProductId).ToList();
        var products = await dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                ImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                    ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);

        return raw
            .Select(r =>
            {
                productLookup.TryGetValue(r.ProductId, out var product);
                return new DashboardTopProductDto(r.ProductId, product?.Name ?? "(produit supprimé)", product?.ImageUrl, r.Units, r.Revenue, r.OrderCount);
            })
            .ToList();
    }

    private async Task<DashboardDeliveryDto> BuildDeliveryAsync(
        DateTime from, DateTime to,
        Dictionary<Guid, DateTime> deliveredAt, Dictionary<Guid, DateTime> outForDeliveryAt, Dictionary<Guid, DateTime> refusedAt,
        Dictionary<Guid, DateTime> returnedAt, Dictionary<Guid, DateTime> deliveryFailedAt, CancellationToken cancellationToken)
    {
        var shipments = await dbContext.Shipments.AsNoTracking()
            .Select(s => new { s.OrderId, s.Carrier })
            .ToListAsync(cancellationToken);
        var carrierByOrder = shipments.ToDictionary(s => s.OrderId, s => s.Carrier);

        var byCarrier = carrierByOrder
            .GroupBy(kv => kv.Value)
            .Select(g => new DashboardCarrierStatDto(
                g.Key,
                g.Count(kv => deliveredAt.TryGetValue(kv.Key, out var d) && InPeriod(d, from, to)),
                g.Count(kv => returnedAt.TryGetValue(kv.Key, out var d) && InPeriod(d, from, to)),
                g.Count(kv => deliveryFailedAt.TryGetValue(kv.Key, out var d) && InPeriod(d, from, to))))
            .Where(c => c.Delivered > 0 || c.Returned > 0 || c.Failed > 0)
            .OrderByDescending(c => c.Delivered)
            .ToList();

        return new DashboardDeliveryDto(
            CountByDate(deliveredAt, from, to),
            CountByDate(outForDeliveryAt, from, to),
            CountByDate(refusedAt, from, to),
            CountByDate(returnedAt, from, to),
            byCarrier);
    }

    private async Task<DashboardReturnsDto> BuildReturnsAsync(
        List<OrderSnapshot> orders, Dictionary<Guid, DateTime> returnedAt, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var returnedIds = orders
            .Where(o => returnedAt.TryGetValue(o.Id, out var d) && InPeriod(d, from, to))
            .Select(o => o.Id)
            .ToHashSet();

        if (returnedIds.Count == 0)
        {
            return new DashboardReturnsDto(0, []);
        }

        var byReasonRaw = await dbContext.Orders.AsNoTracking()
            .Where(o => returnedIds.Contains(o.Id) && o.ReturnReason != null)
            .GroupBy(o => o.ReturnReason)
            .Select(g => new { Reason = g.Key!.Value, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byReason = byReasonRaw
            .OrderByDescending(r => r.Count)
            .Select(r => new DashboardReturnReasonStatDto(r.Reason, r.Count))
            .ToList();

        return new DashboardReturnsDto(returnedIds.Count, byReason);
    }

    private static IReadOnlyList<DashboardActionQueueItemDto> BuildActionQueue(List<OrderSnapshot> orders)
    {
        var currentCounts = orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count());
        int Count(OrderStatus status) => currentCounts.GetValueOrDefault(status);

        return
        [
            new DashboardActionQueueItemDto("to-confirm", "Commandes à confirmer", Count(OrderStatus.PendingConfirmation) + Count(OrderStatus.CustomerUnreachable)),
            new DashboardActionQueueItemDto("to-prepare", "Commandes confirmées à préparer", Count(OrderStatus.Confirmed)),
            new DashboardActionQueueItemDto("to-ship", "Commandes prêtes à expédier", Count(OrderStatus.ReadyToShip)),
            new DashboardActionQueueItemDto("delivery-failed", "Échecs de livraison à traiter", Count(OrderStatus.DeliveryFailed)),
            new DashboardActionQueueItemDto("refused", "Colis refusés à traiter", Count(OrderStatus.Refused)),
        ];
    }
}
