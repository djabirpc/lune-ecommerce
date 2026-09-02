using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Shipping.Dtos;

public record ShipmentTrackingEventDto(
    Guid Id,
    string ProviderStatus,
    NormalizedShippingStatus NormalizedStatus,
    string? Description,
    DateTime OccurredAtUtc);
