import type { PromotionType } from '../api/types';

export const PROMOTION_TYPE_LABELS: Record<PromotionType, string> = {
  ProductDiscount: 'Réduction produit',
  CategoryDiscount: 'Réduction catégorie',
  FlashSale: 'Vente flash',
  PercentageDiscount: 'Réduction en pourcentage',
  FixedAmountDiscount: 'Réduction montant fixe',
  BuyXGetY: 'Achetez X, obtenez Y',
  FreeShipping: 'Livraison gratuite',
  Coupon: 'Code promo',
};
