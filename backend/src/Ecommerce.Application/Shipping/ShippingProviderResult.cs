using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Shipping;

public record ShippingProviderResult(
    string ProviderShipmentId,
    string? TrackingNumber,
    string ProviderStatus,
    NormalizedShippingStatus NormalizedStatus);
