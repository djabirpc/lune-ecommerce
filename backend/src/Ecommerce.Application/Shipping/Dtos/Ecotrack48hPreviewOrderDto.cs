namespace Ecommerce.Application.Shipping.Dtos;

/// <summary>
/// One row from Ecotrack's real GET /get/orders — used only by the read-only admin preview
/// (Ecotrack48hPreviewController), never by the real checkout/shipping flow. Deliberately a separate
/// shape from ShippingTrackingResult/ShipmentDto (the real, wired-in models) so this preview stays
/// fully decoupled from production order/shipment logic per the user's explicit "don't touch the
/// real site yet" request.
/// </summary>
public record Ecotrack48hPreviewOrderDto(
    string Tracking,
    string? Reference,
    string Client,
    string Phone,
    string? Address,
    int? WilayaId,
    string? WilayaName,
    decimal? Amount,
    decimal? DeliveryFee,
    string? Products,
    string Status,
    string GlobalStatus,
    bool StopDesk,
    DateTime? CreatedAtUtc,
    DateTime? LastUpdatedAtUtc,
    string? DriverName);

public record Ecotrack48hPreviewOrdersPageDto(
    IReadOnlyList<Ecotrack48hPreviewOrderDto> Items,
    int CurrentPage,
    int LastPage,
    int Total);
