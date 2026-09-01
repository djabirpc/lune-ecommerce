namespace Ecommerce.Application.Catalog.Dtos;

public record CreateProductRequest(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    IReadOnlyList<CreateProductVariantRequest> Variants);
