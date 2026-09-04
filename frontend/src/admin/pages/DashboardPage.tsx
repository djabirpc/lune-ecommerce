import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { RETURN_REASON_LABELS } from '../../lib/format/orderLabels';
import type { OrderStatus } from '../../lib/api/types';

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

export function DashboardPage() {
  return (
    <div>
      <h1 className="mb-6 text-xl font-semibold">Tableau de bord</h1>

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
        {STAT_STATUSES.map((s) => (
          <StatCard key={s.label} status={s.status} label={s.label} />
        ))}
      </div>

      <ReturnReasonsCard />

      <div className="mt-8 flex flex-wrap gap-3">
        <Link to="/admin/orders" className="rounded-full bg-luna-black px-5 py-2 text-sm text-white">
          Voir les commandes
        </Link>
        <Link to="/admin/products" className="rounded-full border border-luna-black px-5 py-2 text-sm">
          Gérer les produits
        </Link>
      </div>

      <p className="mt-6 text-xs text-luna-charcoal/50">
        Chiffre d'affaires et autres statistiques agrégées : pas encore disponibles (nécessite un endpoint backend
        dédié).
      </p>
    </div>
  );
}
