namespace Ecommerce.Application.Catalog.Dtos;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    int DisplayOrder);
