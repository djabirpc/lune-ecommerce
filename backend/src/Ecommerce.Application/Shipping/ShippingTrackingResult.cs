using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Shipping;

public record ShippingTrackingResult(
    string ProviderStatus,
    NormalizedShippingStatus NormalizedStatus,
    string? Description);
