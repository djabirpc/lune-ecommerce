namespace Ecommerce.Application.Shipping.Dtos;

/// <summary>Aggregated stats across the merchant's whole Ecotrack order history (see
/// Ecotrack48hPreviewService — computed from a short-lived in-memory cache of every page, not
/// refetched per request). Preview-only, same scope note as Ecotrack48hPreviewOrderDto.</summary>
public record Ecotrack48hPreviewWilayaStatDto(int WilayaId, string WilayaName, int Count, decimal TotalAmount);

public record Ecotrack48hPreviewStatusStatDto(string Status, int Count);

public record Ecotrack48hPreviewDashboardDto(
    int TotalOrders,
    decimal TotalAmount,
    int DeliveredCount,
    int InProcessCount,
    int ReturnCount,
    IReadOnlyList<Ecotrack48hPreviewWilayaStatDto> ByWilaya,
    IReadOnlyList<Ecotrack48hPreviewStatusStatDto> ByStatus);
