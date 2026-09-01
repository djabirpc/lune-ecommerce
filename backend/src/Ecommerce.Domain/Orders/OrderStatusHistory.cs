using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Orders;

public class OrderStatusHistory : Entity
{
    public Guid OrderId { get; set; }
    public OrderStatus OldStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Reason { get; set; }

    public Order Order { get; set; } = null!;
}
