import type { OrderStatus } from '../api/types';

// Mirrors Ecommerce.Infrastructure.Orders.OrderService.AllowedTransitions on the backend.
// Kept in sync by hand (no shared codegen) — if the backend map changes, update this too.
export const ALLOWED_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  PendingConfirmation: ['Confirmed', 'CustomerUnreachable', 'Cancelled'],
  // Self-transition, deliberate — lets an agent mark "still unreachable" repeatedly after each
  // follow-up call; each click appends a fresh entry to the order's status history. Order matches
  // PendingConfirmation's (Confirmed, CustomerUnreachable, Cancelled) on purpose — this array drives
  // the action button render order on OrderDetailPage, and the buttons must stay in a fixed position
  // across a Confirm/Unreachable/Cancel call flow instead of visually reshuffling after each click.
  CustomerUnreachable: ['Confirmed', 'CustomerUnreachable', 'Cancelled'],
  // Confirmed -> ReadyToShip directly: the former separate "Preparing" step was merged into this
  // one, so a confirmed order reaches "ready to create a shipment" in one agent click, not two.
  Confirmed: ['ReadyToShip', 'Cancelled'],
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

// 'Returned' is deliberately excluded — it has its own dedicated form (reason dropdown, not a free-text
// prompt) on OrderDetailPage, since the return cause is now a structured OrderReturnReason, not free text.
const STATUSES_REQUIRING_REASON: ReadonlySet<OrderStatus> = new Set([
  'Cancelled',
  'CustomerUnreachable',
  'DeliveryFailed',
  'Refused',
]);

export function requiresReason(status: OrderStatus): boolean {
  return STATUSES_REQUIRING_REASON.has(status);
}
