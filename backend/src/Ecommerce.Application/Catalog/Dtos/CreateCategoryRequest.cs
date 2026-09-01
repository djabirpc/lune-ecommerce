namespace Ecommerce.Application.Catalog.Dtos;

public record CreateCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    int DisplayOrder);
