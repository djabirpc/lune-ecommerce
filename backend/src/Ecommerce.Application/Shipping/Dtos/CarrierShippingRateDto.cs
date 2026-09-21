using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Shipping.Dtos;

public record CarrierShippingRateDto(
    Guid Id,
    ShippingCarrier Carrier,
    string Wilaya,
    decimal? HomeDeliveryPrice,
    decimal? StopDeskPrice,
    string? Notes);

public record SaveCarrierShippingRateRequest(
    ShippingCarrier Carrier,
    string Wilaya,
    decimal? HomeDeliveryPrice,
    decimal? StopDeskPrice,
    string? Notes);
