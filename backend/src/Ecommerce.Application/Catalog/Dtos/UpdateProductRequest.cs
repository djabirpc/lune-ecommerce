namespace Ecommerce.Application.Catalog.Dtos;

public record UpdateProductRequest(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    bool IsActive);
