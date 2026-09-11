namespace Ecommerce.Domain.Promotions;

public enum PromotionType
{
    ProductDiscount,
    CategoryDiscount,
    FlashSale,
    PercentageDiscount,
    FixedAmountDiscount,
    BuyXGetY,
    FreeShipping,
    Coupon,
    /// <summary>
    /// "N for a fixed total price" (e.g. "2 for 1500 DA" on a 1000 DA item) — added beyond CLAUDE.md
    /// section 20's original 8 types, per direct user request. See BundleQuantity/BundleTotalPrice
    /// on <see cref="Promotion"/> and OrderService.ComputeBundlePriceDiscount.
    /// </summary>
    BundlePrice,
}
