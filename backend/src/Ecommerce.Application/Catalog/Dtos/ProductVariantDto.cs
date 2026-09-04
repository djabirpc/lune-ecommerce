namespace Ecommerce.Application.Catalog.Dtos;

public record ProductVariantDto(
    Guid Id,
    string Color,
    string Size,
    string Sku,
    decimal Price,
    decimal? CostPrice,
    bool IsActive,
    int AvailableQuantity);
