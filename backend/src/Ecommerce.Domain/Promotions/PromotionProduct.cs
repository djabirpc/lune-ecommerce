namespace Ecommerce.Domain.Promotions;

public class PromotionProduct
{
    public Guid PromotionId { get; set; }
    public Guid ProductId { get; set; }

    public Promotion Promotion { get; set; } = null!;
}
