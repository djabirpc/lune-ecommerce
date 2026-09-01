import type { DeliveryType, OrderStatus } from '../api/types';

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  PendingConfirmation: 'En attente de confirmation',
  Confirmed: 'Confirmée',
  Preparing: 'En préparation',
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

export const DELIVERY_TYPE_LABELS: Record<DeliveryType, string> = {
  HomeDelivery: 'Livraison à domicile',
  StopDesk: 'Point relais (Stop Desk)',
};
