using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Shipping.Dtos;

public record ShippingQuoteDto(string Wilaya, DeliveryType DeliveryType, decimal Price);
