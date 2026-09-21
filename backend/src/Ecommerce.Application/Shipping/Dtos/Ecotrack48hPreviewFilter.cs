namespace Ecommerce.Application.Shipping.Dtos;

/// <summary>
/// Shared filter for both GetOrdersAsync (table) and GetDashboardAsync (stat cards/map/top-wilayas) —
/// applied against the single in-memory cached snapshot of the whole account
/// (Ecotrack48hPreviewService.GetAllOrdersAsync), never as separate Ecotrack API calls. By convention
/// the dashboard is only ever queried with Status/WilayaId left null (see Ecotrack48hPreviewPage.tsx)
/// so the map/stat cards keep showing the full picture while the table narrows further — nothing on
/// the backend enforces that split, it's just how the one frontend consumer uses it.
/// </summary>
public record Ecotrack48hPreviewFilter(
    string? Status = null,
    int? WilayaId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? Search = null);
