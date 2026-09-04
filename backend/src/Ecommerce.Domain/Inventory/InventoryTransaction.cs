using Ecommerce.Domain.Catalog;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Inventory;

public class InventoryTransaction : Entity
{
    public Guid ProductVariantId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }

    /// <summary>Who this stock was purchased from — only meaningful for <see cref="InventoryTransactionType.Restock"/>.</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>Purchase price per unit for this specific restock — costs can vary by supplier/batch, so this is captured per-transaction rather than only on the variant.</summary>
    public decimal? UnitCost { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;
    public Supplier? Supplier { get; set; }
}
