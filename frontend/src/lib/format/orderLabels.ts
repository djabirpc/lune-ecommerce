import { useTranslation } from 'react-i18next';

import type { CallAttemptResult, DeliveryType, OrderReturnReason, OrderStatus } from '../api/types';

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  PendingConfirmation: 'En attente de confirmation',
  Confirmed: 'Confirmée',
  ReadyToShip: 'Prête à expédier',
  Shipped: 'Expédiée',
  OutForDelivery: 'En cours de livraison',
  Delivered: 'Livrée',
  Cancelled: 'Annulée',
  CustomerUnreachable: 'Injoignable',
  DeliveryFailed: 'Échec de livraison',
  Refused: 'Refusée',
  Returned: 'Retournée',
};

// Fixed, reserved status→color mapping reused across every admin chart/chip that encodes OrderStatus
// (dashboard charts, funnel, action queue) — never reassigned per-view, so the same status always
// reads the same color throughout the app.
export const ORDER_STATUS_COLORS: Record<OrderStatus, string> = {
  PendingConfirmation: '#d97706',
  CustomerUnreachable: '#b45309',
  Confirmed: '#0284c7',
  ReadyToShip: '#0369a1',
  Shipped: '#4f46e5',
  OutForDelivery: '#4338ca',
  Delivered: '#059669',
  Returned: '#e11d48',
  Refused: '#dc2626',
  DeliveryFailed: '#b91c1c',
  Cancelled: '#6b7280',
};

export const DELIVERY_TYPE_LABELS: Record<DeliveryType, string> = {
  HomeDelivery: 'Livraison à domicile',
  StopDesk: 'Point relais (Stop Desk)',
};

export const CALL_ATTEMPT_RESULT_LABELS: Record<CallAttemptResult, string> = {
  NoAnswer: 'Pas de réponse',
  Confirmed: 'Confirmée',
  Cancelled: 'Annulée',
  CallbackScheduled: 'Rappel programmé',
};

export const RETURN_REASON_LABELS: Record<OrderReturnReason, string> = {
  Damaged: 'Colis endommagé',
  WrongSize: 'Mauvaise taille',
  WrongItem: 'Mauvais article',
  CustomerChangedMind: 'Client a changé d\'avis',
  Other: 'Autre',
};

// Storefront-facing translated variants (FR/AR) — admin stays French-only per CLAUDE.md section 45,
// so admin pages keep using the plain consts above.
export function useOrderStatusLabels(): Record<OrderStatus, string> {
  const { t } = useTranslation();
  return {
    PendingConfirmation: t('orderStatus.pendingConfirmation'),
    Confirmed: t('orderStatus.confirmed'),
    ReadyToShip: t('orderStatus.readyToShip'),
    Shipped: t('orderStatus.shipped'),
    OutForDelivery: t('orderStatus.outForDelivery'),
    Delivered: t('orderStatus.delivered'),
    Cancelled: t('orderStatus.cancelled'),
    CustomerUnreachable: t('orderStatus.customerUnreachable'),
    DeliveryFailed: t('orderStatus.deliveryFailed'),
    Refused: t('orderStatus.refused'),
    Returned: t('orderStatus.returned'),
  };
}

export function useDeliveryTypeLabels(): Record<DeliveryType, string> {
  const { t } = useTranslation();
  return {
    HomeDelivery: t('deliveryType.home'),
    StopDesk: t('deliveryType.stopDesk'),
  };
}
