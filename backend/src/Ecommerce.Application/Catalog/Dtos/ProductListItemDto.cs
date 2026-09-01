namespace Ecommerce.Application.Catalog.Dtos;

public record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    string? PrimaryImageUrl,
    string CategoryName,
    string CategorySlug,
    bool IsActive);
