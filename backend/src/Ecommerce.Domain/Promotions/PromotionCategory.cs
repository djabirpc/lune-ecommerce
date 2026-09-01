namespace Ecommerce.Domain.Promotions;

public class PromotionCategory
{
    public Guid PromotionId { get; set; }
    public Guid CategoryId { get; set; }

    public Promotion Promotion { get; set; } = null!;
}
