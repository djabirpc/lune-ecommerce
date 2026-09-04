using Ecommerce.Domain.Common;
using Ecommerce.Domain.Inventory;

namespace Ecommerce.Domain.Catalog;

public class ProductVariant : Entity
{
    public Guid ProductId { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal? PriceOverride { get; set; }
    public decimal? CostPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
    public InventoryRecord? Inventory { get; set; }
}
