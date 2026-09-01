using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Orders;

public class OrderPromotion : Entity
{
    public Guid OrderId { get; set; }
    public Guid? PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }

    public Order Order { get; set; } = null!;
}
