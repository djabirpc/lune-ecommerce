namespace Ecommerce.Application.Catalog.Dtos;

public record ProductImageDto(
    Guid Id,
    string Url,
    string? AltText,
    int DisplayOrder,
    bool IsPrimary);
