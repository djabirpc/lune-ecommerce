using Ecommerce.Application.Shipping.Dtos;

namespace Ecommerce.Application.Shipping;

/// <summary>
/// Read-only preview of the merchant's real 48h Express (Ecotrack) account — lets an admin browse
/// packages already created directly on the Ecotrack platform, without the real Luna order/shipping
/// flow being involved at all. Purely GET calls against Ecotrack's API; never creates, updates, or
/// deletes anything there. See Ecotrack48hPreviewController for the admin-only route.
/// </summary>
public interface IEcotrack48hPreviewService
{
    Task<Ecotrack48hPreviewOrdersPageDto> GetOrdersAsync(
        Ecotrack48hPreviewFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Ecotrack48hPreviewTrackingDto> GetTrackingAsync(string tracking, CancellationToken cancellationToken = default);

    Task<Ecotrack48hPreviewDashboardDto> GetDashboardAsync(Ecotrack48hPreviewFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Raw PDF bytes for a real order's shipping label, via Ecotrack's documented
    /// GET /get/order/label?tracking= endpoint. Still a GET — no different from any other preview call.</summary>
    Task<byte[]> GetLabelPdfAsync(string tracking, CancellationToken cancellationToken = default);
}
