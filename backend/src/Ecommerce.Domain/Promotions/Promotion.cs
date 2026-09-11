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

    /// <summary>BundlePrice only: buy this many matching units in one line to trigger the bundle price.</summary>
    public int? BundleQuantity { get; set; }
    /// <summary>BundlePrice only: total price charged for one complete bundle of BundleQuantity units.</summary>
    public decimal? BundleTotalPrice { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }

    public ICollection<PromotionProduct> Products { get; set; } = [];
    public ICollection<PromotionCategory> Categories { get; set; } = [];
}
