using Ecommerce.Domain.Marketing;

namespace Ecommerce.Application.Marketing.Dtos;

/// <summary>Never carries the actual access tokens — only whether one is set — so this DTO is safe
/// to return from an authenticated GET without re-exposing a secret that was written once.</summary>
public record MarketingSettingsDto(
    PurchaseEventTrigger PurchaseEventTrigger,
    string? MetaPixelId,
    bool IsMetaAccessTokenConfigured,
    string? TikTokPixelCode,
    bool IsTikTokAccessTokenConfigured);

/// <summary>The only part of the settings the public storefront needs — whether to fire the "Purchase"
/// pixel event itself right after creating the order, or leave it to the server at confirmation time.</summary>
public record PurchaseTriggerDto(PurchaseEventTrigger Trigger);
