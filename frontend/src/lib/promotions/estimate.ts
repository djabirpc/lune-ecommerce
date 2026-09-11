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
): boolean {
  return activePromotions
    .filter((p) => p.type === 'FreeShipping')
    .some((p) => isScopedTo(p, productId, categoryId));
}

/**
 * The active "N for a fixed total price" offer for a product, if any (e.g. "2 pour 1500 DA").
 * Unlike estimatePrice, this isn't a per-unit price change — it only makes sense with the real
 * quantity/unit-price math the backend applies at checkout (OrderService.ComputeBundlePriceDiscount),
 * so it's surfaced as its own informational banner rather than folded into the product-card badge.
 */
export function findBundleOffer(
  activePromotions: PromotionDto[],
  productId: string,
  categoryId: string,
): PromotionDto | undefined {
  return activePromotions
    .filter((p) => p.type === 'BundlePrice' && p.bundleQuantity && p.bundleTotalPrice)
    .filter((p) => isScopedTo(p, productId, categoryId))
    .sort((a, b) => b.priority - a.priority)[0];
}

export interface CartDiscountEstimate {
  discountTotal: number;
  promotionNames: string[];
}

function computeLineDiscount(promotion: PromotionDto, item: CartItem): number {
  if (promotion.type === 'BuyXGetY') {
    if (!promotion.buyQuantity || !promotion.getQuantity) return 0;
    const bundleSize = promotion.buyQuantity + promotion.getQuantity;
    const completeBundles = Math.floor(item.quantity / bundleSize);
    return completeBundles * promotion.getQuantity * item.unitPrice;
  }

  if (promotion.type === 'BundlePrice') {
    if (!promotion.bundleQuantity || !promotion.bundleTotalPrice) return 0;
    const completeBundles = Math.floor(item.quantity / promotion.bundleQuantity);
    if (completeBundles === 0) return 0;
    const regularPrice = completeBundles * promotion.bundleQuantity * item.unitPrice;
    const bundlePrice = completeBundles * promotion.bundleTotalPrice;
    return Math.max(regularPrice - bundlePrice, 0);
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
 * OrderService.CalculatePromotionsAsync's per-line best-priority-promotion selection and
 * ComputeItemDiscount's dispatch (percentage/fixed, BuyXGetY bundle math, BundlePrice bundle math)
 * closely enough to show a real number in CartPage/CheckoutPage before the order is placed. Like
 * estimatePrice, this is a preview only — the backend recalculates authoritatively at order
 * creation (CLAUDE.md section 41). Coupon codes are excluded (Important Decision #41 — never
 * previewed before the customer types the code); FreeShipping doesn't affect item pricing so it's
 * out of scope here too (the live shipping-cost quote already covers that separately).
 */
export function estimateCartDiscount(items: CartItem[], activePromotions: PromotionDto[]): CartDiscountEstimate {
  const candidates = activePromotions.filter((p) => CART_LINE_PROMOTION_TYPES.has(p.type));
  let discountTotal = 0;
  const promotionNames = new Set<string>();

  for (const item of items) {
    const applicable = candidates
      .filter((p) => isScopedTo(p, item.productId, item.categoryId))
      .sort((a, b) => b.priority - a.priority)[0];

    if (!applicable) continue;

    const discount = computeLineDiscount(applicable, item);
    if (discount <= 0) continue;

    discountTotal += discount;
    promotionNames.add(applicable.name);
  }

  return { discountTotal: Math.round(discountTotal * 100) / 100, promotionNames: [...promotionNames] };
}
