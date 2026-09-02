using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Shipping.Dtos;

public record ShipmentDto(
    Guid Id,
    Guid OrderId,
    ShippingCarrier Carrier,
    string ProviderShipmentId,
    string? TrackingNumber,
    string ProviderStatus,
    NormalizedShippingStatus NormalizedStatus,
    DateTime CreatedAtUtc,
    IReadOnlyList<ShipmentTrackingEventDto> TrackingEvents);
