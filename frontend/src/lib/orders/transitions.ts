import type { OrderStatus } from '../api/types';

// Mirrors Ecommerce.Infrastructure.Orders.OrderService.AllowedTransitions on the backend.
// Kept in sync by hand (no shared codegen) — if the backend map changes, update this too.
export const ALLOWED_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  PendingConfirmation: ['Confirmed', 'CustomerUnreachable', 'Cancelled'],
  CustomerUnreachable: ['Confirmed', 'Cancelled'],
  Confirmed: ['Preparing', 'Cancelled'],
  Preparing: ['ReadyToShip', 'Cancelled'],
  ReadyToShip: ['Shipped', 'Cancelled'],
  Shipped: ['OutForDelivery'],
  OutForDelivery: ['Delivered', 'DeliveryFailed', 'Refused'],
  DeliveryFailed: ['OutForDelivery', 'Returned', 'Cancelled'],
  Refused: ['Returned'],
  Delivered: ['Returned'],
  Cancelled: [],
  Returned: [],
};

export const ORDER_ACTION_LABELS: Record<OrderStatus, string> = {
  PendingConfirmation: 'Remettre en attente',
  Confirmed: 'Confirmer',
  Preparing: 'Marquer en préparation',
  ReadyToShip: 'Marquer prête à expédier',
  Shipped: 'Marquer expédiée',
  OutForDelivery: 'Marquer en cours de livraison',
  Delivered: 'Marquer livrée',
  Cancelled: 'Annuler',
  CustomerUnreachable: 'Marquer injoignable',
  DeliveryFailed: "Marquer échec de livraison",
  Refused: 'Marquer refusée',
  Returned: 'Marquer retournée',
};

const STATUSES_REQUIRING_REASON: ReadonlySet<OrderStatus> = new Set([
  'Cancelled',
  'CustomerUnreachable',
  'DeliveryFailed',
  'Refused',
  'Returned',
]);

export function requiresReason(status: OrderStatus): boolean {
  return STATUSES_REQUIRING_REASON.has(status);
}
