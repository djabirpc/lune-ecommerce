import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { formatPrice } from '../../lib/format/price';
import { ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';

const PAGE_SIZE = 10;

// Order history for one product — matched server-side by the product's slug against each
// OrderItem's snapshotted ProductSlug, so it survives even if a specific variant is later deleted
// (see ProductOrderHistoryItemDto's doc comment on the backend).
export function ProductOrderHistoryPanel({ productId }: { productId: string }) {
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-product-order-history', productId, page],
    queryFn: () => ordersApi.getByProduct(productId, { page, pageSize: PAGE_SIZE }),
  });

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;

  return (
    <div className="border-t border-black/10 bg-luna-cream/30 p-3">
      {isLoading && <p className="text-xs text-luna-charcoal/60">Chargement...</p>}
      {isError && <p className="text-xs text-red-600">Impossible de charger l'historique des commandes.</p>}

      {data && data.items.length === 0 && (
        <p className="text-xs text-luna-charcoal/50">Ce produit n'a encore été commandé dans aucune commande.</p>
      )}

      {data && data.items.length > 0 && (
        <>
          <table className="w-full text-left text-xs">
            <thead className="text-luna-charcoal/60">
              <tr>
                <th className="px-3 py-1">Commande</th>
                <th className="px-3 py-1">Date</th>
                <th className="px-3 py-1">Client</th>
                <th className="px-3 py-1">Quantité</th>
                <th className="px-3 py-1">Montant</th>
                <th className="px-3 py-1">Statut</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((item) => (
                <tr key={item.orderId} className="border-b border-black/5 last:border-0">
                  <td className="px-3 py-2 font-mono">{item.orderNumber}</td>
                  <td className="px-3 py-2">{new Date(item.createdAtUtc).toLocaleDateString('fr-FR')}</td>
                  <td className="px-3 py-2">{item.customerFullName}</td>
                  <td className="px-3 py-2">{item.quantity}</td>
                  <td className="px-3 py-2">{formatPrice(item.lineTotal)}</td>
                  <td className="px-3 py-2">
                    <span className="rounded-full bg-luna-cream px-2 py-0.5">{ORDER_STATUS_LABELS[item.status]}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {totalPages > 1 && (
            <div className="mt-2 flex items-center justify-between text-xs">
              <span className="text-luna-charcoal/60">
                Page {page} / {totalPages} ({data.totalCount} commandes)
              </span>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                  className="rounded border border-black/20 px-2 py-0.5 disabled:opacity-40"
                >
                  Précédent
                </button>
                <button
                  type="button"
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  className="rounded border border-black/20 px-2 py-0.5 disabled:opacity-40"
                >
                  Suivant
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
