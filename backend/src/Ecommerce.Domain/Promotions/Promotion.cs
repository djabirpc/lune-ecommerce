using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Promotions;

public class Promotion : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromotionType Type { get; set; }

    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmountValue { get; set; }
    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }
    public string? CouponCode { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }

    public ICollection<PromotionProduct> Products { get; set; } = [];
    public ICollection<PromotionCategory> Categories { get; set; } = [];
}
