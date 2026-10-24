using Ecommerce.Domain.Marketing;

namespace Ecommerce.Application.Marketing.Dtos;

/// <summary>
/// Token fields use "leave empty to keep the current value" semantics (the admin UI never gets the
/// real secret back to pre-fill, so it can't send it back unchanged) — a null/empty MetaAccessToken
/// means "don't touch it", not "clear it". Use the explicit Clear* flags to actually remove a token.
/// </summary>
public record UpdateMarketingSettingsRequest(
    PurchaseEventTrigger PurchaseEventTrigger,
    string? MetaPixelId,
    string? MetaAccessToken,
    bool ClearMetaAccessToken,
    string? TikTokPixelCode,
    string? TikTokAccessToken,
    bool ClearTikTokAccessToken);
