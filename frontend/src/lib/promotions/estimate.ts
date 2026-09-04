import type { PromotionDto, ProductListItemDto } from '../api/types';

export interface PriceEstimate {
  compareAtPrice: number;
  discountedPrice: number;
  percent: number;
}

const PRICE_DISCOUNT_TYPES = new Set(['ProductDiscount', 'CategoryDiscount', 'FlashSale', 'PercentageDiscount', 'FixedAmountDiscount']);

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
