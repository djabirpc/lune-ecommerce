namespace Ecommerce.Application.Catalog.Dtos;

public record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    string? PrimaryImageUrl,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    bool IsActive,
    DateTime CreatedAtUtc,
    IReadOnlyList<string> Colors,
    IReadOnlyList<string> Sizes,
    bool IsInStock);
