using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Orders;

public class OrderItem : Entity
{
    public Guid OrderId { get; set; }
    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    /// <summary>Snapshotted at order time, like the other fields here — the product could be renamed/deleted later.</summary>
    public string ProductSlug { get; set; } = string.Empty;

    /// <summary>Snapshotted primary product image URL at order time; null if the product had no image yet.</summary>
    public string? ImageUrl { get; set; }

    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public Order Order { get; set; } = null!;
}
