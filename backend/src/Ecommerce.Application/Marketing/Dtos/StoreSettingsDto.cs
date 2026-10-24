namespace Ecommerce.Application.Marketing.Dtos;

/// <summary>Nothing secret here — unlike MarketingSettingsDto, this is safe to return from the
/// public, unauthenticated GET the storefront itself calls.</summary>
public record StoreSettingsDto(
    string? InstagramUrl,
    string? TikTokUrl,
    string? FacebookUrl,
    string? ContactPhone,
    string? AnnouncementTextFr,
    string? AnnouncementTextAr);

public record UpdateStoreSettingsRequest(
    string? InstagramUrl,
    string? TikTokUrl,
    string? FacebookUrl,
    string? ContactPhone,
    string? AnnouncementTextFr,
    string? AnnouncementTextAr);
