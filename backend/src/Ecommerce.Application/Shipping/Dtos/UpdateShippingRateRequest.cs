namespace Ecommerce.Application.Shipping.Dtos;

public record UpdateShippingRateRequest(
    decimal HomeDeliveryPrice,
    decimal StopDeskPrice,
    bool IsActive);
