using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Orders;

public class OrderItem : Entity
{
    public Guid OrderId { get; set; }
    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public Order Order { get; set; } = null!;
}
