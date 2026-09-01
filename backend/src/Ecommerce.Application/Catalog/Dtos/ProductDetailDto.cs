namespace Ecommerce.Application.Catalog.Dtos;

public record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    bool IsActive,
    string CategoryName,
    string CategorySlug,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductVariantDto> Variants);
