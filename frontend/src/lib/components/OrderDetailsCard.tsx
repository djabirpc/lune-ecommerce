import type { OrderDetailDto } from '../api/types';
import { formatPrice } from '../format/price';
import { DELIVERY_TYPE_LABELS, ORDER_STATUS_LABELS } from '../format/orderLabels';

export function OrderDetailsCard({ order }: { order: OrderDetailDto }) {
  return (
    <div className="flex flex-col gap-4 rounded-lg border border-black/10 p-4">
      <div className="flex items-center justify-between">
        <span className="font-mono text-sm">{order.orderNumber}</span>
        <span className="rounded-full bg-luna-cream px-3 py-1 text-xs font-medium">
          {ORDER_STATUS_LABELS[order.status]}
        </span>
      </div>

      <div className="flex flex-col divide-y divide-black/5">
        {order.items.map((item) => (
          <div key={item.id} className="flex items-center justify-between py-2 text-sm">
            <span>
              {item.productName} ({item.color}/{item.size}) × {item.quantity}
            </span>
            <span>{formatPrice(item.lineTotal)}</span>
          </div>
        ))}
      </div>

      {order.discountTotal > 0 && (
        <div className="flex flex-col gap-1 text-sm">
          <div className="flex items-center justify-between text-luna-charcoal/70">
            <span>Sous-total</span>
            <span>{formatPrice(order.subtotal)}</span>
          </div>
          <div className="flex items-center justify-between text-green-700">
            <span>
              Réduction
              {order.appliedPromotions.length > 0 && ` (${order.appliedPromotions.map((p) => p.promotionName).join(', ')})`}
            </span>
            <span>−{formatPrice(order.discountTotal)}</span>
          </div>
        </div>
      )}

      <div className="flex items-center justify-between border-t border-black/10 pt-3 text-sm font-medium">
        <span>Total (paiement à la livraison)</span>
        <span>{formatPrice(order.total)}</span>
      </div>

      <div className="text-sm text-luna-charcoal/70">
        <p>
          {order.firstName} {order.lastName} — {order.phone}
        </p>
        <p>
          {order.address}, {order.commune}, {order.wilaya}
        </p>
        <p>{DELIVERY_TYPE_LABELS[order.deliveryType]}</p>
      </div>
    </div>
  );
}
