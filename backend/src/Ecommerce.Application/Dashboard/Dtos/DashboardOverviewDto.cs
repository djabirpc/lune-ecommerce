using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Dashboard.Dtos;

public record DashboardPeriodDto(DateTime From, DateTime To);

/// <summary>Value for the requested period, the same metric for the immediately-preceding equivalent
/// period, and the resulting percent change. PreviousValue/ChangePercent are null when the comparison
/// period has no data to compare against (e.g. the shop had zero orders back then) — the frontend must
/// not render a comparison in that case rather than showing a misleading 0%/∞ change.</summary>
public record DashboardKpiDto(decimal Value, decimal? PreviousValue, decimal? ChangePercent);

public record DashboardKpisDto(
    DashboardKpiDto Revenue,
    DashboardKpiDto OrdersCreated,
    DashboardKpiDto Confirmed,
    DashboardKpiDto Delivered,
    DashboardKpiDto Returned,
    DashboardKpiDto Cancelled);

public record DashboardRevenuePointDto(DateTime Date, decimal Revenue);

public record DashboardStatusCountDto(OrderStatus Status, int Count);

/// <summary>One stage of the "period-activity" funnel: how many orders reached this stage at some
/// point during the selected period (not a cohort-conversion funnel across time). ConversionPercent is
/// this stage's Count divided by the FIRST stage's Count (orders created in the period) — null for the
/// first stage itself. See DashboardService's doc comment for why this shape was chosen.</summary>
public record DashboardFunnelStageDto(string Key, string Label, int Count, decimal? ConversionPercent);

public record DashboardTopProductDto(Guid ProductId, string ProductName, string? ImageUrl, int UnitsSold, decimal Revenue, int OrderCount);

public record DashboardCarrierStatDto(ShippingCarrier Carrier, int Delivered, int Returned, int Failed);

public record DashboardDeliveryDto(int Delivered, int OutForDelivery, int Refused, int Returned, IReadOnlyList<DashboardCarrierStatDto> ByCarrier);

public record DashboardReturnReasonStatDto(OrderReturnReason Reason, int Count);

public record DashboardReturnsDto(int TotalReturned, IReadOnlyList<DashboardReturnReasonStatDto> ByReason);

public record DashboardMarketingSourceStatDto(string Source, int OrderCount, decimal Revenue);

public record DashboardStockAlertDto(Guid ProductVariantId, Guid ProductId, string ProductName, string Color, string Size, int AvailableQuantity);

/// <summary>Current-state operational counts — deliberately NOT period-filtered (see CLAUDE.md's
/// dashboard task: "current stock is a current-state metric", same reasoning applies to any queue of
/// work still outstanding right now). Key is a stable frontend-facing slug used to route to the
/// relevant filtered admin page (e.g. "to-confirm" → /admin/orders/confirmation).</summary>
public record DashboardActionQueueItemDto(string Key, string Label, int Count);

public record DashboardOverviewDto(
    DashboardPeriodDto Period,
    DashboardPeriodDto ComparisonPeriod,
    bool HasComparisonData,
    DashboardKpisDto Kpis,
    IReadOnlyList<DashboardRevenuePointDto> Revenue,
    string RevenueGranularity,
    IReadOnlyList<DashboardStatusCountDto> OrdersByStatus,
    IReadOnlyList<DashboardFunnelStageDto> Funnel,
    IReadOnlyList<DashboardTopProductDto> TopProducts,
    DashboardDeliveryDto Delivery,
    DashboardReturnsDto Returns,
    IReadOnlyList<DashboardMarketingSourceStatDto> Marketing,
    IReadOnlyList<DashboardActionQueueItemDto> ActionQueue,
    IReadOnlyList<DashboardStockAlertDto> LowStock);
