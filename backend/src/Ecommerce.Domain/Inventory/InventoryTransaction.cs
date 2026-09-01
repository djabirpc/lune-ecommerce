using Ecommerce.Domain.Catalog;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Inventory;

public class InventoryTransaction : Entity
{
    public Guid ProductVariantId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;
}
