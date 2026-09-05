import { Link } from 'react-router-dom';

import type { OrderDetailDto } from '../api/types';
import { formatPrice } from '../format/price';
import { DELIVERY_TYPE_LABELS, ORDER_STATUS_LABELS } from '../format/orderLabels';
import { OrderTimeline } from './OrderTimeline';

export function OrderDetailsCard({ order, showTimeline = true }: { order: OrderDetailDto; showTimeline?: boolean }) {
  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-sm border border-black/10 bg-white p-5">
        <div className="flex items-center justify-between">
          <span className="font-mono text-sm text-luna-charcoal/80">{order.orderNumber}</span>
          <span className="rounded-full bg-luna-rose px-3 py-1 text-xs font-medium text-luna-accent-dark">
            {ORDER_STATUS_LABELS[order.status]}
          </span>
        </div>

        {showTimeline && (
          <div className="mt-5 border-t border-black/10 pt-5">
            <OrderTimeline status={order.status} />
          </div>
        )}
      </div>

      <div className="rounded-sm border border-black/10 bg-white p-5">
        <h2 className="font-display text-xl text-luna-black">Récapitulatif</h2>
        <ul className="mt-4 space-y-3">
          {order.items.map((item) => (
            <li key={item.id} className="flex gap-3">
              <Link to={`/product/${item.productSlug}`} className="h-16 w-12 shrink-0 overflow-hidden rounded-sm bg-luna-cream-dark">
                {item.imageUrl && <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />}
              </Link>
              <div className="min-w-0 flex-1 text-xs">
                <Link to={`/product/${item.productSlug}`} className="line-clamp-1 text-luna-black">
                  {item.productName}
                </Link>
                <p className="text-luna-charcoal/60">
                  {item.color} · {item.size} · x{item.quantity}
                </p>
              </div>
              <span className="text-xs font-medium text-luna-black">{formatPrice(item.lineTotal)}</span>
            </li>
          ))}
        </ul>

        <dl className="mt-5 space-y-2 border-t border-black/10 pt-4 text-sm">
          <div className="flex justify-between">
            <dt className="text-luna-charcoal/60">Sous-total</dt>
            <dd>{formatPrice(order.subtotal)}</dd>
          </div>
          {order.discountTotal > 0 && (
            <div className="flex justify-between text-luna-accent-dark">
              <dt>
                Réduction
                {order.appliedPromotions.length > 0 && ` (${order.appliedPromotions.map((p) => p.promotionName).join(', ')})`}
              </dt>
              <dd>−{formatPrice(order.discountTotal)}</dd>
            </div>
          )}
          <div className="flex justify-between">
            <dt className="text-luna-charcoal/60">Livraison</dt>
            <dd>{order.shippingCost > 0 ? formatPrice(order.shippingCost) : 'Offerte'}</dd>
          </div>
          <div className="flex justify-between border-t border-black/10 pt-3 text-base font-medium text-luna-black">
            <dt>Total à payer</dt>
            <dd>{formatPrice(order.total)}</dd>
          </div>
        </dl>
      </div>

      <div className="rounded-sm border border-black/10 bg-white p-5 text-sm">
        <h2 className="font-display text-xl text-luna-black">Livraison</h2>
        <p className="mt-3 text-luna-black">
          {order.firstName} {order.lastName}
        </p>
        <p className="text-luna-charcoal/60">{order.phone}</p>
        <p className="text-luna-charcoal/60">
          {order.address}, {order.commune}, {order.wilaya}
        </p>
        <p className="mt-1 text-luna-charcoal/60">{DELIVERY_TYPE_LABELS[order.deliveryType]}</p>
        {order.notes && <p className="mt-2 text-xs text-luna-charcoal/50">Note : {order.notes}</p>}
      </div>
    </div>
  );
}
