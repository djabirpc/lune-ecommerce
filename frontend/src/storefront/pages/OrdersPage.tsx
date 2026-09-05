import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Package } from 'lucide-react';

import { ordersApi } from '../../lib/api/orders';
import { getOrderHistory } from '../../lib/orders/localOrderHistory';
import { formatPrice } from '../../lib/format/price';
import { ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';

export function OrdersPage() {
  const history = getOrderHistory();

  const { data: orders, isLoading } = useQuery({
    queryKey: ['local-order-history', history.map((h) => h.orderNumber).join(',')],
    queryFn: async () => {
      const results = await Promise.allSettled(
        history.map((entry) => ordersApi.track(entry.orderNumber, entry.phone)),
      );
      return results.filter((r) => r.status === 'fulfilled').map((r) => r.value);
    },
    enabled: history.length > 0,
  });

  if (history.length === 0) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center px-4 py-24 text-center">
        <Package className="h-10 w-10 text-luna-charcoal/40" />
        <h1 className="mt-4 font-display text-3xl text-luna-black">Aucune commande</h1>
        <p className="mt-2 text-sm text-luna-charcoal/70">
          Vos commandes passées sur cet appareil apparaîtront ici avec leur suivi de livraison.
        </p>
        <Link to="/categories" className="mt-6 rounded-sm bg-luna-black px-7 py-3 text-sm text-white">
          Découvrir la collection
        </Link>
      </div>
    );
  }

  if (isLoading) {
    return <div className="px-4 py-24 text-center text-sm text-luna-charcoal/60">Chargement…</div>;
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      <p className="eyebrow">Mon espace</p>
      <h1 className="mt-1 font-display text-4xl text-luna-black">Mes commandes</h1>

      <ul className="mt-6 space-y-4">
        {orders?.map((order) => (
          <li key={order.id} className="rounded-sm border border-black/10 bg-white p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <p className="text-sm font-medium text-luna-black">{order.orderNumber}</p>
                <p className="text-xs text-luna-charcoal/60">{new Date(order.createdAtUtc).toLocaleDateString('fr-FR')}</p>
              </div>
              <span className="rounded-full bg-luna-rose px-3 py-1 text-xs text-luna-accent-dark">
                {ORDER_STATUS_LABELS[order.status]}
              </span>
            </div>

            <div className="no-scrollbar mt-3 flex gap-2 overflow-x-auto">
              {order.items.map((item) => (
                <div key={item.id} className="h-16 w-12 shrink-0 overflow-hidden rounded-sm bg-luna-cream-dark">
                  {item.imageUrl && <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />}
                </div>
              ))}
            </div>

            <div className="mt-3 flex items-center justify-between">
              <span className="text-sm font-medium text-luna-black">{formatPrice(order.total)}</span>
              <Link
                to={`/orders/${order.orderNumber}`}
                className="rounded-full border border-luna-black px-4 py-1.5 text-xs text-luna-black"
              >
                Suivre
              </Link>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
