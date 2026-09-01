namespace Ecommerce.Application.Orders.Dtos;

public record OrderItemRequest(Guid ProductVariantId, int Quantity);
