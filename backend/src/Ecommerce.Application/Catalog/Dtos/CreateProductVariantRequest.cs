namespace Ecommerce.Application.Catalog.Dtos;

public record CreateProductVariantRequest(
    string Color,
    string Size,
    string Sku,
    decimal? PriceOverride,
    int InitialQuantity,
    decimal? CostPrice = null);
