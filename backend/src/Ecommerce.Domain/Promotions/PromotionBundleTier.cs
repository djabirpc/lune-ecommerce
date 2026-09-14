namespace Ecommerce.Domain.Promotions;

/// <summary>
/// One "N for a fixed total price" tier belonging to a BundlePrice promotion. A single promotion can
/// have several tiers (e.g. "2 for 1500 DA" and "4 for 3000 DA" on the same product) — checkout picks
/// whichever tier is most advantageous for the actual quantity purchased (see
/// OrderService.CalculatePromotionsAsync), and each tier can independently grant free shipping.
/// </summary>
public class PromotionBundleTier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;

    /// <summary>Buy this many matching units in one line to trigger this tier's bundle price.</summary>
    public int BundleQuantity { get; set; }
    /// <summary>Total price charged for one complete bundle of BundleQuantity units.</summary>
    public decimal BundleTotalPrice { get; set; }
    /// <summary>
    /// When true, this tier also grants free shipping on the whole order once it applies (at least
    /// one complete bundle) — lets a tier grant free shipping directly instead of needing a separate
    /// paired FreeShipping promotion with a matching MinQuantity.
    /// </summary>
    public bool IncludesFreeShipping { get; set; }
}
