import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import type { OrderStatus, OrderSummaryDto } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';
import { ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';

const PAGE_SIZE = 50;

function OrderQueue({ title, status }: { title: string; status: OrderStatus }) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-orders-confirmation', status],
    queryFn: () => ordersApi.getPaged({ status, page: 1, pageSize: PAGE_SIZE }),
  });

  return (
    <div>
      <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">
        {title} {data ? `(${data.totalCount})` : ''}
      </h2>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
      {isError && <p className="text-sm text-red-600">Impossible de charger les commandes.</p>}

      {data && (
        <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/10 text-xs uppercase text-luna-charcoal/60">
              <tr>
                <th className="px-4 py-2">Numéro</th>
                <th className="px-4 py-2">Client</th>
                <th className="px-4 py-2">Téléphone</th>
                <th className="px-4 py-2">Wilaya</th>
                <th className="px-4 py-2">Total</th>
                <th className="px-4 py-2">Date</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((order: OrderSummaryDto) => (
                <tr key={order.id} className="border-b border-black/5 last:border-0 hover:bg-luna-cream/50">
                  <td className="px-4 py-2">
                    <Link to={`/admin/orders/${order.id}`} className="font-mono text-xs underline">
                      {order.orderNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-2">{order.customerFullName}</td>
                  <td className="px-4 py-2">{order.phone}</td>
                  <td className="px-4 py-2">{order.wilaya}</td>
                  <td className="px-4 py-2">{formatPrice(order.total)}</td>
                  <td className="px-4 py-2 text-xs text-luna-charcoal/60">
                    {new Date(order.createdAtUtc).toLocaleString('fr-FR')}
                  </td>
                </tr>
              ))}
              {data.items.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-luna-charcoal/60">
                    Aucune commande.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export function OrderConfirmationCenterPage() {
  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold">Centre de confirmation</h1>

      <div className="flex flex-col gap-8">
        <OrderQueue title={ORDER_STATUS_LABELS.PendingConfirmation} status="PendingConfirmation" />
        <OrderQueue title={ORDER_STATUS_LABELS.CustomerUnreachable} status="CustomerUnreachable" />
      </div>
    </div>
  );
}
