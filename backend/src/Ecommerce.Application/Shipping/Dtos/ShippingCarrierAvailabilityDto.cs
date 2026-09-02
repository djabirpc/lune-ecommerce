using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Shipping.Dtos;

public record ShippingCarrierAvailabilityDto(ShippingCarrier Carrier, bool IsConfigured, string? UnavailableReason);
