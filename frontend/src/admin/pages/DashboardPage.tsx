import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { formatPrice } from '../../lib/format/price';
import { ORDER_STATUS_LABELS, RETURN_REASON_LABELS } from '../../lib/format/orderLabels';
import type { OrderStatus } from '../../lib/api/types';
import { Drawer } from '../components/Drawer';
import { OrderSummaryPanel } from '../components/OrderSummaryPanel';

const STAT_STATUSES: { status: OrderStatus | undefined; label: string }[] = [
  { status: undefined, label: 'Total commandes' },
  { status: 'PendingConfirmation', label: 'En attente de confirmation' },
  { status: 'Confirmed', label: 'Confirmées' },
  { status: 'Delivered', label: 'Livrées' },
  { status: 'Returned', label: 'Retours' },
  { status: 'Cancelled', label: 'Annulées' },
];

function StatCard({ status, label }: { status: OrderStatus | undefined; label: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['order-count', status],
    queryFn: () => ordersApi.getPaged({ status, pageSize: 1 }),
  });

  return (
    <div className="rounded-lg border border-black/10 bg-white p-4">
      <p className="text-xs uppercase text-luna-charcoal/60">{label}</p>
      <p className="mt-1 text-2xl font-semibold">{isLoading ? '…' : data?.totalCount}</p>
    </div>
  );
}

function ReturnReasonsCard() {
  const { data, isLoading } = useQuery({
    queryKey: ['return-reason-summary'],
    queryFn: () => ordersApi.getReturnReasonSummary(),
  });

  if (isLoading || !data || data.length === 0) return null;

  return (
    <div className="mt-6 rounded-lg border border-black/10 bg-white p-4 sm:max-w-md">
      <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Retours par cause</h2>
      <div className="flex flex-col divide-y divide-black/5 text-sm">
        {data.map((r) => (
          <div key={r.reason} className="flex items-center justify-between py-1.5">
            <span>{RETURN_REASON_LABELS[r.reason]}</span>
            <span className="font-medium">{r.count}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function RecentOrdersCard({ onSelect }: { onSelect: (orderId: string) => void }) {
  const { data, isLoading } = useQuery({
    queryKey: ['recent-orders'],
    queryFn: () => ordersApi.getPaged({ pageSize: 6 }),
  });

  return (
    <div className="mt-6 rounded-lg border border-black/10 bg-white sm:max-w-md">
      <h2 className="border-b border-black/10 px-4 py-3 text-sm font-semibold uppercase text-luna-charcoal/60">
        Commandes récentes
      </h2>
      {isLoading && <p className="px-4 py-4 text-sm text-luna-charcoal/60">Chargement...</p>}
      {data && data.items.length === 0 && <p className="px-4 py-4 text-sm text-luna-charcoal/60">Aucune commande.</p>}
      {data && data.items.length > 0 && (
        <div className="divide-y divide-black/5">
          {data.items.map((order) => (
            <button
              key={order.id}
              type="button"
              onClick={() => onSelect(order.id)}
              className="flex w-full items-center justify-between gap-3 px-4 py-2.5 text-left text-sm hover:bg-luna-cream/50"
            >
              <div className="min-w-0">
                <p className="truncate">{order.customerFullName}</p>
                <p className="text-xs text-luna-charcoal/60">{order.orderNumber}</p>
              </div>
              <div className="shrink-0 text-right">
                <p className="font-medium">{formatPrice(order.total)}</p>
                <p className="text-xs text-luna-charcoal/60">{ORDER_STATUS_LABELS[order.status]}</p>
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export function DashboardPage() {
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);

  return (
    <div>
      <h1 className="mb-6 text-xl font-semibold">Tableau de bord</h1>

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
        {STAT_STATUSES.map((s) => (
          <StatCard key={s.label} status={s.status} label={s.label} />
        ))}
      </div>

      <ReturnReasonsCard />
      <RecentOrdersCard onSelect={setSelectedOrderId} />

      <div className="mt-8 flex flex-wrap gap-3">
        <Link to="/admin/orders" className="rounded-full bg-luna-black px-5 py-2 text-sm text-white">
          Voir les commandes
        </Link>
        <Link to="/admin/products" className="rounded-full border border-luna-black px-5 py-2 text-sm">
          Gérer les produits
        </Link>
      </div>

      <Drawer open={!!selectedOrderId} onClose={() => setSelectedOrderId(null)} title="Résumé de la commande">
        {selectedOrderId && <OrderSummaryPanel orderId={selectedOrderId} onClose={() => setSelectedOrderId(null)} />}
      </Drawer>

      <p className="mt-6 text-xs text-luna-charcoal/50">
        Chiffre d'affaires et autres statistiques agrégées : pas encore disponibles (nécessite un endpoint backend
        dédié).
      </p>
    </div>
  );
}
