using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Marketing;

/// <summary>
/// Single-row configuration table (see MarketingSettingsService.GetOrCreateAsync) — there is exactly
/// one of these for the whole store, edited from /admin/settings. Access tokens are real secrets:
/// never returned by the API once set (only an "is configured" boolean), never logged, never sent to
/// the frontend build like VITE_META_PIXEL_ID is.
/// </summary>
public class MarketingSettings : Entity
{
    public PurchaseEventTrigger PurchaseEventTrigger { get; set; } = PurchaseEventTrigger.OrderCreated;

    /// <summary>Not secret (same as the existing Product.FacebookPixelId) — Meta's Pixel ID is meant to be public.</summary>
    public string? MetaPixelId { get; set; }

    /// <summary>Secret. A Conversions API system-user access token — distinct from the Pixel ID.</summary>
    public string? MetaAccessToken { get; set; }

    /// <summary>Not secret — TikTok's "Pixel Code" identifier.</summary>
    public string? TikTokPixelCode { get; set; }

    /// <summary>Secret. A TikTok Events API access token.</summary>
    public string? TikTokAccessToken { get; set; }
}
