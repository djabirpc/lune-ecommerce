using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Shipping.Dtos;

public record CreateShipmentRequest(ShippingCarrier Carrier);
