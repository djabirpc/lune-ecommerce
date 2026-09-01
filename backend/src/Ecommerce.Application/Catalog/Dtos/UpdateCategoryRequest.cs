namespace Ecommerce.Application.Catalog.Dtos;

public record UpdateCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    int DisplayOrder);
