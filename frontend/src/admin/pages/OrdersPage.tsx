import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import type { OrderStatus } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';
import { ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';

const PAGE_SIZE = 20;
const ALL_STATUSES: OrderStatus[] = [
  'PendingConfirmation',
  'Confirmed',
  'Preparing',
  'ReadyToShip',
  'Shipped',
  'OutForDelivery',
  'Delivered',
  'Cancelled',
  'CustomerUnreachable',
  'DeliveryFailed',
  'Refused',
  'Returned',
];

export function OrdersPage() {
  const [status, setStatus] = useState<OrderStatus | ''>('');
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-orders', { status, page }],
    queryFn: () => ordersApi.getPaged({ status: status || undefined, page, pageSize: PAGE_SIZE }),
  });

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-semibold">Commandes</h1>
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as OrderStatus | '');
            setPage(1);
          }}
          className="rounded border border-black/20 px-3 py-2 text-sm"
        >
          <option value="">Tous les statuts</option>
          {ALL_STATUSES.map((s) => (
            <option key={s} value={s}>
              {ORDER_STATUS_LABELS[s]}
            </option>
          ))}
        </select>
      </div>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
      {isError && <p className="text-sm text-red-600">Impossible de charger les commandes.</p>}

      {data && (
        <>
          <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-black/10 text-xs uppercase text-luna-charcoal/60">
                <tr>
                  <th className="px-4 py-2">Numéro</th>
                  <th className="px-4 py-2">Client</th>
                  <th className="px-4 py-2">Téléphone</th>
                  <th className="px-4 py-2">Wilaya</th>
                  <th className="px-4 py-2">Total</th>
                  <th className="px-4 py-2">Statut</th>
                  <th className="px-4 py-2">Date</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((order) => (
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
                    <td className="px-4 py-2">
                      <span className="rounded-full bg-luna-cream px-2 py-1 text-xs">
                        {ORDER_STATUS_LABELS[order.status]}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-xs text-luna-charcoal/60">
                      {new Date(order.createdAtUtc).toLocaleString('fr-FR')}
                    </td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-8 text-center text-luna-charcoal/60">
                      Aucune commande.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="mt-4 flex items-center justify-between text-sm">
            <span className="text-luna-charcoal/60">
              Page {page} / {totalPages} ({data.totalCount} commandes)
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
                className="rounded border border-black/20 px-3 py-1 disabled:opacity-40"
              >
                Précédent
              </button>
              <button
                type="button"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
                className="rounded border border-black/20 px-3 py-1 disabled:opacity-40"
              >
                Suivant
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
