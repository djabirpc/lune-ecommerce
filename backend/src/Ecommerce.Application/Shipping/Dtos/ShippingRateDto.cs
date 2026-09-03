namespace Ecommerce.Application.Shipping.Dtos;

public record ShippingRateDto(
    string Wilaya,
    decimal HomeDeliveryPrice,
    decimal StopDeskPrice,
    bool IsActive);
