import type { PromotionDto, ProductListItemDto, PromotionType } from '../api/types';
import type { CartItem } from '../cart/CartContext';

export interface PriceEstimate {
  compareAtPrice: number;
  discountedPrice: number;
  percent: number;
}

const PRICE_DISCOUNT_TYPES = new Set(['ProductDiscount', 'CategoryDiscount', 'FlashSale', 'PercentageDiscount', 'FixedAmountDiscount']);

// Every automatic (non-coupon, non-FreeShipping) type that can discount a cart line — mirrors the
// candidate filter in OrderService.CalculatePromotionsAsync.
const CART_LINE_PROMOTION_TYPES: ReadonlySet<PromotionType> = new Set([
  'ProductDiscount',
  'CategoryDiscount',
  'FlashSale',
  'PercentageDiscount',
  'FixedAmountDiscount',
  'BuyXGetY',
  'BundlePrice',
]);

function isScopedTo(promotion: PromotionDto, productId: string, categoryId: string): boolean {
  if (promotion.productIds.length === 0 && promotion.categoryIds.length === 0) return true;
  return promotion.productIds.includes(productId) || promotion.categoryIds.includes(categoryId);
}

/**
 * Client-side preview of a product's active automatic discount, for display on product
 * cards/pages only. Mirrors OrderService.ComputeDiscount's percentage/fixed logic exactly, but the
 * backend always recalculates authoritatively at order creation (CLAUDE.md section 41) — this is
 * never what actually gets charged. Coupon codes are deliberately excluded (Important Decision #41:
 * never previewed pre-checkout, since they require an explicit code the customer hasn't entered yet).
 */
export function estimatePrice(
  product: Pick<ProductListItemDto, 'id' | 'categoryId' | 'price'>,
  activePromotions: PromotionDto[],
): PriceEstimate | null {
  const applicable = activePromotions
    .filter((p) => PRICE_DISCOUNT_TYPES.has(p.type))
    .filter((p) => isScopedTo(p, product.id, product.categoryId))
    .sort((a, b) => b.priority - a.priority)[0];

  if (!applicable) return null;

  const discount = applicable.percentageValue
    ? Math.round(((product.price * applicable.percentageValue) / 100) * 100) / 100
    : applicable.fixedAmountValue
      ? Math.min(applicable.fixedAmountValue, product.price)
      : 0;

  if (discount <= 0) return null;

  const discountedPrice = product.price - discount;
  return {
    compareAtPrice: product.price,
    discountedPrice,
    percent: Math.round((discount / product.price) * 100),
  };
}

/** The single active FlashSale promotion, if any (for the homepage/promotions countdown section). */
export function findFlashSale(activePromotions: PromotionDto[]): PromotionDto | undefined {
  return activePromotions.find((p) => p.type === 'FlashSale');
}

export function isFreeShippingActiveFor(
  activePromotions: PromotionDto[],
  productId: string,
  categoryId: string,
  quantity: number,
): boolean {
  return activePromotions
    .filter((p) => p.type === 'FreeShipping')
    .filter((p) => isScopedTo(p, productId, categoryId))
    .some((p) => !p.minQuantity || quantity >= p.minQuantity);
}

/**
 * The FreeShipping promotion (if any) scoped to this product that requires a minimum quantity —
 * used to surface "free shipping from N items" as a second tier alongside a BundlePrice offer (e.g.
 * "2 for 1500 DA" + "4 items also ships free"). Promotions with no minimum aren't returned here since
 * they don't need a quantity threshold to be surfaced as their own tier.
 */
export function findFreeShippingThreshold(
  activePromotions: PromotionDto[],
  productId: string,
  categoryId: string,
): PromotionDto | undefined {
  return activePromotions
    .filter((p) => p.type === 'FreeShipping' && p.minQuantity && p.minQuantity > 0)
    .filter((p) => isScopedTo(p, productId, categoryId))
    .sort((a, b) => a.minQuantity! - b.minQuantity!)[0];
}

export interface BundleTierOption {
  promotionId: string;
  promotionName: string;
  tierId: string;
  bundleQuantity: number;
  bundleTotalPrice: number;
  includesFreeShipping: boolean;
}

/**
 * Every active "N for a fixed total price" tier for a product, flattened across every active
 * BundlePrice promotion scoped to it (a single promotion can itself have several tiers, e.g.
 * "2 pour 1500 DA" AND "4 pour 3000 DA" together), sorted ascending by tier quantity so they render
 * smallest-first. Unlike estimatePrice, this isn't a per-unit price change — it only makes sense with
 * the real quantity/unit-price math the backend applies at checkout
 * (OrderService.ComputeBundleTierDiscount), so each tier is surfaced as its own informational banner
 * rather than folded into the product-card badge.
 */
export function findBundleTierOptions(
  activePromotions: PromotionDto[],
  productId: string,
  categoryId: string,
): BundleTierOption[] {
  return activePromotions
    .filter((p) => p.type === 'BundlePrice')
    .filter((p) => isScopedTo(p, productId, categoryId))
    .flatMap((p) =>
      p.bundleTiers.map((t) => ({
        promotionId: p.id,
        promotionName: p.name,
        tierId: t.id,
        bundleQuantity: t.bundleQuantity,
        bundleTotalPrice: t.bundleTotalPrice,
        includesFreeShipping: t.includesFreeShipping,
      })),
    )
    .sort((a, b) => a.bundleQuantity - b.bundleQuantity);
}

/**
 * Client-side preview of whether any active FreeShipping promotion covers the current cart —
 * mirrors OrderService.IsFreeShippingEligible: sums the quantity of items scoped to each candidate
 * promotion and, if it sets a MinQuantity, requires that threshold to be reached. Preview only; the
 * backend recalculates authoritatively at order creation (CLAUDE.md section 41) — this exists so the
 * checkout summary doesn't show a shipping fee the order won't actually be charged.
 */
export function estimateFreeShipping(items: CartItem[], activePromotions: PromotionDto[]): boolean {
  return activePromotions
    .filter((p) => p.type === 'FreeShipping')
    .some((p) => {
      const scopedQuantity = items
        .filter((i) => isScopedTo(p, i.productId, i.categoryId))
        .reduce((sum, i) => sum + i.quantity, 0);
      if (scopedQuantity === 0) return false;
      return !p.minQuantity || scopedQuantity >= p.minQuantity;
    });
}

export interface CartDiscountEstimate {
  discountTotal: number;
  promotionNames: string[];
}

/**
 * "N for a fixed total price" bundle math, shared between the cart-line estimate below and
 * ProductPage's pre-add-to-cart total preview. Complete bundles only — a remainder that doesn't
 * reach bundleQuantity stays at full price, never partially discounted (mirrors
 * OrderService.ComputeBundlePriceDiscount exactly).
 */
export function computeBundlePriceDiscount(
  bundleQuantity: number,
  bundleTotalPrice: number,
  unitPrice: number,
  quantity: number,
): number {
  const completeBundles = Math.floor(quantity / bundleQuantity);
  if (completeBundles === 0) return 0;
  const regularPrice = completeBundles * bundleQuantity * unitPrice;
  const bundlePrice = completeBundles * bundleTotalPrice;
  return Math.max(regularPrice - bundlePrice, 0);
}

function computeLineDiscount(promotion: PromotionDto, item: CartItem): number {
  if (promotion.type === 'BuyXGetY') {
    if (!promotion.buyQuantity || !promotion.getQuantity) return 0;
    const bundleSize = promotion.buyQuantity + promotion.getQuantity;
    const completeBundles = Math.floor(item.quantity / bundleSize);
    return completeBundles * promotion.getQuantity * item.unitPrice;
  }

  if (promotion.type === 'BundlePrice') {
    // Best tier within this promotion for the actual quantity — mirrors
    // OrderService.ComputeBestBundleTierDiscount (several tiers, e.g. "2 for 1500" and "4 for 3000",
    // can belong to the same promotion).
    return promotion.bundleTiers.reduce(
      (best, tier) => Math.max(best, computeBundlePriceDiscount(tier.bundleQuantity, tier.bundleTotalPrice, item.unitPrice, item.quantity)),
      0,
    );
  }

  const lineTotal = item.unitPrice * item.quantity;
  if (promotion.percentageValue) {
    return Math.round(((lineTotal * promotion.percentageValue) / 100) * 100) / 100;
  }
  if (promotion.fixedAmountValue) {
    return Math.min(promotion.fixedAmountValue, lineTotal);
  }
  return 0;
}

/**
 * Client-side preview of the cart's total discount across all lines — mirrors
 * OrderService.CalculatePromotionsAsync's per-line best-discount-for-the-quantity promotion
 * selection (not simply highest Priority — lets multiple BundlePrice tiers on the same product
 * coexist, e.g. "2 for 1500" and "4 for 3000", each cart automatically getting whichever tier is
 * more advantageous) and ComputeItemDiscount's dispatch (percentage/fixed, BuyXGetY bundle math,
 * BundlePrice bundle math) closely enough to show a real number in CartPage/CheckoutPage before the
 * order is placed. Like estimatePrice, this is a preview only — the backend recalculates
 * authoritatively at order creation (CLAUDE.md section 41). Coupon codes are excluded (Important
 * Decision #41 — never previewed before the customer types the code); FreeShipping doesn't affect
 * item pricing so it's out of scope here too (the live shipping-cost quote already covers that
 * separately).
 */
export function estimateCartDiscount(items: CartItem[], activePromotions: PromotionDto[]): CartDiscountEstimate {
  const candidates = activePromotions.filter((p) => CART_LINE_PROMOTION_TYPES.has(p.type));
  let discountTotal = 0;
  const promotionNames = new Set<string>();

  for (const item of items) {
    const best = candidates
      .filter((p) => isScopedTo(p, item.productId, item.categoryId))
      .map((p) => ({ promotion: p, discount: computeLineDiscount(p, item) }))
      .filter((x) => x.discount > 0)
      .sort((a, b) => b.discount - a.discount || b.promotion.priority - a.promotion.priority)[0];

    if (!best) continue;

    discountTotal += best.discount;
    promotionNames.add(best.promotion.name);
  }

  return { discountTotal: Math.round(discountTotal * 100) / 100, promotionNames: [...promotionNames] };
}
