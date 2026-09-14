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

    /// <summary>
    /// BundlePrice only: the promotion's tiers (e.g. "2 for 1500 DA" and "4 for 3000 DA" together).
    /// See PromotionBundleTier.
    /// </summary>
    public ICollection<PromotionBundleTier> BundleTiers { get; set; } = [];

    /// <summary>
    /// FreeShipping only: minimum total quantity of scoped items in the order required to unlock free
    /// shipping. Null means any quantity qualifies. Lets a merchant pair a BundlePrice offer (e.g.
    /// "2 for 1500 DA") with a higher-tier reward (e.g. "4+ items also ships free") using two separate
    /// promotions on the same product, rather than inventing a multi-tier bundle model.
    /// </summary>
    public int? MinQuantity { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }

    public ICollection<PromotionProduct> Products { get; set; } = [];
    public ICollection<PromotionCategory> Categories { get; set; } = [];
}
