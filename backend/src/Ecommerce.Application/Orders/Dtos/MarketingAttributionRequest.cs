namespace Ecommerce.Application.Orders.Dtos;

public record MarketingAttributionRequest(
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? UtmContent,
    string? UtmTerm,
    string? Fbclid,
    string? Ttclid,
    string? Referrer,
    string? LandingPage);
