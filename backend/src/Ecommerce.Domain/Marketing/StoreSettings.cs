using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Marketing;

/// <summary>
/// Single-row configuration table (same pattern as MarketingSettings) for general storefront
/// content that previously only lived in build-time env vars (VITE_INSTAGRAM_URL/VITE_TIKTOK_URL/
/// VITE_FACEBOOK_URL/VITE_STORE_PHONE) or hardcoded i18n strings (the announcement bar) — editable
/// from /admin/settings without a redeploy. Nothing here is secret; the GET endpoint is public.
/// </summary>
public class StoreSettings : Entity
{
    public string? InstagramUrl { get; set; }
    public string? TikTokUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? ContactPhone { get; set; }

    /// <summary>Null means "use the built-in i18n default for each language" (CLAUDE.md section 45 —
    /// storefront stays bilingual even when nothing's configured here) rather than showing a blank bar.</summary>
    public string? AnnouncementTextFr { get; set; }
    public string? AnnouncementTextAr { get; set; }
}
