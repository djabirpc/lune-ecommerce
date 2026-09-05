namespace Ecommerce.Application.Orders.Dtos;

public record OrderItemDto(
    Guid Id,
    Guid ProductVariantId,
    string ProductName,
    string ProductSlug,
    string? ImageUrl,
    string Color,
    string Size,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
