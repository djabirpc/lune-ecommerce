namespace Ecommerce.Application.Inventory.Dtos;

public record InventoryVariantOverviewDto(
    Guid ProductVariantId,
    string Color,
    string Size,
    string Sku,
    decimal? CostPrice,
    decimal Price,
    int AvailableQuantity,
    int ReservedQuantity,
    int SoldQuantity,
    int ReturnedQuantity,
    int DamagedQuantity);

/// <summary>One product, with every one of its active variants' stock — the admin stock page is
/// organized by product/category first, drilling into variants per product, rather than one flat
/// row per variant.</summary>
public record InventoryProductOverviewDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    Guid CategoryId,
    string CategoryName,
    string? ImageUrl,
    IReadOnlyList<InventoryVariantOverviewDto> Variants);

public enum InventoryStockStatus
{
    All,
    Low,
    Out,
}

/// <summary>Search/stockStatus match at the VARIANT level (e.g. "this SKU" or "any low-stock
/// variant"), but a matching product is returned with ALL of its variants, not just the matching
/// one — once an admin opens a product they need the full picture, not a partial one.</summary>
public record InventoryOverviewFilter(
    string? Search = null,
    Guid? CategoryId = null,
    InventoryStockStatus StockStatus = InventoryStockStatus.All);
