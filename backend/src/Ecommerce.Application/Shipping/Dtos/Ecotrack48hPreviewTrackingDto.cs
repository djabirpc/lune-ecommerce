namespace Ecommerce.Application.Shipping.Dtos;

/// <summary>
/// GET /get/tracking/info's real shape — a customer-facing activity timeline, used by the preview to
/// show what a "client" tracking view could look like. Read-only, preview-only (see
/// Ecotrack48hPreviewOrderDto's comment).
/// </summary>
public record Ecotrack48hPreviewTrackingEventDto(string? Status, string? Station, DateTime? OccurredAtUtc);

public record Ecotrack48hPreviewTrackingDto(
    string Tracking,
    string? RecipientName,
    string? CurrentStation,
    IReadOnlyList<Ecotrack48hPreviewTrackingEventDto> Activity);
