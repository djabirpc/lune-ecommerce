namespace Ecommerce.Application.Catalog.Dtos;

public record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    bool IsActive,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductVariantDto> Variants);
