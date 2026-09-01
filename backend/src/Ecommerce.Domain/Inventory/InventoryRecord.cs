using Ecommerce.Domain.Catalog;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Inventory;

public class InventoryRecord : Entity
{
    public Guid ProductVariantId { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public int DamagedQuantity { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;
}
