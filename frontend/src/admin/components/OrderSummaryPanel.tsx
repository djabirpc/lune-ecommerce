import { Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Phone } from 'lucide-react';

import { ordersApi } from '../../lib/api/orders';
import type { OrderDetailDto } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';
import { CALL_ATTEMPT_RESULT_LABELS, DELIVERY_TYPE_LABELS, ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';
import { NORMALIZED_SHIPPING_STATUS_LABELS, SHIPPING_CARRIER_LABELS } from '../../lib/format/shippingLabels';
import { OrderActionsPanel } from './OrderActionsPanel';
import { OrderStatusTimeline } from './OrderStatusTimeline';

interface ActivityEntry {
  id: string;
  timestamp: string;
  label: string;
  actor: string | null;
}

// Merges statusHistory + callAttempts into one reverse-chronological feed — both are already on
// OrderDetailDto, nothing new to fetch. "Commande créée" is synthesized from createdAtUtc rather
// than a StatusHistory row, since a status transition only exists *between* two statuses and there
// is no "from nothing to PendingConfirmation" row for a brand new order.
function buildActivityFeed(order: OrderDetailDto): ActivityEntry[] {
  const entries: ActivityEntry[] = [
    { id: `created-${order.id}`, timestamp: order.createdAtUtc, label: 'Commande créée', actor: null },
    ...order.statusHistory.map((h) => ({
      id: h.id,
      timestamp: h.createdAtUtc,
      label: `${ORDER_STATUS_LABELS[h.oldStatus]} → ${ORDER_STATUS_LABELS[h.newStatus]}`,
      actor: h.changedByUserName,
    })),
    ...order.callAttempts.map((a) => ({
      id: a.id,
      timestamp: a.calledAtUtc,
      label: `Appel #${a.attemptNumber} — ${CALL_ATTEMPT_RESULT_LABELS[a.result]}`,
      actor: a.agentUserName,
    })),
  ];

  return entries.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
}

export function OrderSummaryPanel({ orderId, onClose }: { orderId: string; onClose: () => void }) {
  const queryClient = useQueryClient();

  const { data: order, isLoading, isError } = useQuery({
    queryKey: ['admin-order', orderId],
    queryFn: () => ordersApi.getById(orderId),
  });

  function handleOrderChanged(updated: OrderDetailDto) {
    queryClient.setQueryData(['admin-order', orderId], updated);
    queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
  }

  if (isLoading) {
    return <p className="text-sm text-luna-charcoal/60">Chargement...</p>;
  }

  if (isError || !order) {
    return <p className="text-sm text-red-600">Impossible de charger cette commande.</p>;
  }

  const activity = buildActivityFeed(order).slice(0, 6);

  return (
    <div className="flex flex-col gap-5">
      <div>
        <div className="flex items-center justify-between gap-2">
          <span className="font-mono text-sm font-medium">{order.orderNumber}</span>
          <span className="rounded-full bg-luna-cream px-2.5 py-1 text-xs font-medium">{ORDER_STATUS_LABELS[order.status]}</span>
        </div>
        <p className="mt-1 text-lg font-semibold text-luna-black">
          {order.firstName} {order.lastName}
        </p>
        <p className="text-xs text-luna-charcoal/60">{new Date(order.createdAtUtc).toLocaleString('fr-FR')}</p>
      </div>

      <div className="rounded-lg border border-black/10 bg-white p-4">
        <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Client</h3>
        <p className="text-sm">
          {order.firstName} {order.lastName}
        </p>
        <a href={`tel:${order.phone}`} className="mt-1 inline-flex items-center gap-1.5 text-sm text-luna-accent-dark underline">
          <Phone className="h-3.5 w-3.5" /> {order.phone}
        </a>
        <p className="mt-2 text-sm text-luna-charcoal/80">
          {[order.address, order.commune, order.wilaya].filter(Boolean).join(', ')}
        </p>
        <p className="text-sm text-luna-charcoal/60">{DELIVERY_TYPE_LABELS[order.deliveryType]}</p>
        {order.notes && <p className="mt-2 text-xs text-luna-charcoal/60">Remarque : {order.notes}</p>}
      </div>

      <div className="rounded-lg border border-black/10 bg-white p-4">
        <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Articles</h3>
        <div className="flex flex-col divide-y divide-black/5">
          {order.items.map((item) => (
            <div key={item.id} className="flex items-center gap-3 py-2">
              <div className="h-12 w-9 shrink-0 overflow-hidden rounded bg-luna-cream-dark">
                {item.imageUrl && <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />}
              </div>
              <div className="min-w-0 flex-1 text-sm">
                <p className="truncate">{item.productName}</p>
                <p className="text-xs text-luna-charcoal/60">
                  {item.color} · {item.size} · x{item.quantity}
                </p>
              </div>
              <span className="shrink-0 text-sm font-medium">{formatPrice(item.lineTotal)}</span>
            </div>
          ))}
        </div>

        <div className="mt-3 space-y-1 border-t border-black/10 pt-3 text-sm">
          <div className="flex justify-between">
            <span className="text-luna-charcoal/60">Sous-total</span>
            <span>{formatPrice(order.subtotal)}</span>
          </div>
          {order.discountTotal > 0 && (
            <div className="flex justify-between text-green-700">
              <span>Réduction</span>
              <span>−{formatPrice(order.discountTotal)}</span>
            </div>
          )}
          <div className="flex justify-between">
            <span className="text-luna-charcoal/60">Livraison</span>
            <span>{formatPrice(order.shippingCost)}</span>
          </div>
          <div className="flex justify-between border-t border-black/10 pt-1.5 text-base font-medium">
            <span>Total</span>
            <span>{formatPrice(order.total)}</span>
          </div>
          <p className="text-xs text-luna-charcoal/60">
            {order.paymentMethod} — {order.paymentStatus}
          </p>
        </div>
      </div>

      <div className="rounded-lg border border-black/10 bg-white p-4">
        <h3 className="mb-3 text-xs font-semibold uppercase text-luna-charcoal/60">Suivi</h3>
        <OrderStatusTimeline status={order.status} />
      </div>

      <div className="rounded-lg border border-black/10 bg-white p-4 text-sm">
        <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Livraison</h3>
        {order.shipment ? (
          <>
            <p>
              {SHIPPING_CARRIER_LABELS[order.shipment.carrier]} — {NORMALIZED_SHIPPING_STATUS_LABELS[order.shipment.normalizedStatus]}
            </p>
            {order.shipment.trackingNumber && (
              <p className="text-xs text-luna-charcoal/60">Numéro de suivi : {order.shipment.trackingNumber}</p>
            )}
          </>
        ) : (
          <p className="text-luna-charcoal/60">Aucune expédition assignée pour le moment.</p>
        )}
      </div>

      {activity.length > 0 && (
        <div className="rounded-lg border border-black/10 bg-white p-4">
          <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Activité récente</h3>
          <div className="flex flex-col divide-y divide-black/5 text-sm">
            {activity.map((entry) => (
              <div key={entry.id} className="py-1.5">
                <p>{entry.label}</p>
                <p className="text-xs text-luna-charcoal/60">
                  {new Date(entry.timestamp).toLocaleString('fr-FR')}
                  {entry.actor ? ` · ${entry.actor}` : ''}
                </p>
              </div>
            ))}
          </div>
        </div>
      )}

      <div>
        <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Actions</h3>
        <OrderActionsPanel order={order} onChanged={handleOrderChanged} />
      </div>

      <Link
        to={`/admin/orders/${order.id}`}
        onClick={onClose}
        className="rounded-full border border-luna-black px-4 py-2.5 text-center text-sm font-medium hover:bg-luna-cream"
      >
        Voir tous les détails
      </Link>
    </div>
  );
}
