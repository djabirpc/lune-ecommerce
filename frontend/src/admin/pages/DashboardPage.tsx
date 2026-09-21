import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { dashboardApi } from '../../lib/api/dashboard';
import { formatPrice } from '../../lib/format/price';
import { ORDER_STATUS_LABELS, RETURN_REASON_LABELS } from '../../lib/format/orderLabels';
import type { DashboardKpiDto, DashboardOverviewDto } from '../../lib/api/types';
import { DASHBOARD_PERIOD_PRESETS, formatPeriodLabel, toApiDate } from '../lib/dashboardPeriods';
import { Drawer } from '../components/Drawer';
import { OrderSummaryPanel } from '../components/OrderSummaryPanel';
import { PeriodFilter } from '../components/dashboard/PeriodFilter';
import { RevenueChart } from '../components/dashboard/RevenueChart';
import { StatusBarChart } from '../components/dashboard/StatusBarChart';

// Deep-links for the "à faire" operational queue — see DashboardActionQueueItemDto's doc comment on
// the backend for why these keys/counts are current-state and never period-filtered.
const ACTION_QUEUE_ROUTES: Record<string, string> = {
  'to-confirm': '/admin/orders/confirmation',
  'to-prepare': '/admin/orders?status=Confirmed',
  'to-ship': '/admin/orders?status=ReadyToShip',
  'delivery-failed': '/admin/orders?status=DeliveryFailed',
  refused: '/admin/orders?status=Refused',
};

function SectionCard({ title, children, className = '' }: { title: string; children: React.ReactNode; className?: string }) {
  return (
    <div className={`rounded-lg border border-black/10 bg-white p-4 ${className}`}>
      <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">{title}</h2>
      {children}
    </div>
  );
}

function Skeleton({ className = '' }: { className?: string }) {
  return <div className={`animate-pulse rounded bg-luna-cream-dark/60 ${className}`} />;
}

function KpiDelta({ kpi, hasComparisonData }: { kpi: DashboardKpiDto; hasComparisonData: boolean }) {
  if (!hasComparisonData) return null;

  if (kpi.changePercent === null) {
    return <p className="mt-1 text-xs text-luna-charcoal/40">Pas de comparaison disponible</p>;
  }

  const positive = kpi.changePercent >= 0;
  return (
    <p className={`mt-1 text-xs font-medium ${positive ? 'text-emerald-600' : 'text-rose-600'}`}>
      {positive ? '▲' : '▼'} {Math.abs(kpi.changePercent).toFixed(1)}% vs période précédente
    </p>
  );
}

function KpiCard({
  label,
  kpi,
  hasComparisonData,
  formatValue,
}: {
  label: string;
  kpi: DashboardKpiDto;
  hasComparisonData: boolean;
  formatValue: (v: number) => string;
}) {
  return (
    <div className="rounded-lg border border-black/10 bg-white p-4">
      <p className="text-xs uppercase text-luna-charcoal/60">{label}</p>
      <p className="mt-1 text-2xl font-semibold tabular-nums">{formatValue(kpi.value)}</p>
      <KpiDelta kpi={kpi} hasComparisonData={hasComparisonData} />
    </div>
  );
}

function ActionQueueCard({ items }: { items: DashboardOverviewDto['actionQueue'] }) {
  const active = items.filter((i) => i.count > 0);

  return (
    <SectionCard title="À faire" className="flex flex-col">
      {active.length === 0 && <p className="text-sm text-luna-charcoal/50">Rien à traiter pour le moment.</p>}
      <div className="flex flex-col divide-y divide-black/5">
        {active.map((item) => {
          const route = ACTION_QUEUE_ROUTES[item.key];
          const content = (
            <div className="flex items-center justify-between py-2.5 text-sm">
              <span>{item.label}</span>
              <span className="rounded-full bg-luna-black px-2.5 py-0.5 text-xs font-semibold text-white">{item.count}</span>
            </div>
          );
          return route ? (
            <Link key={item.key} to={route} className="hover:bg-luna-cream/50">
              {content}
            </Link>
          ) : (
            <div key={item.key}>{content}</div>
          );
        })}
      </div>
    </SectionCard>
  );
}

function StockAlertsCard({ items }: { items: DashboardOverviewDto['lowStock'] }) {
  return (
    <SectionCard title="Alertes stock faible">
      {items.length === 0 && <p className="text-sm text-luna-charcoal/50">Aucun stock faible actuellement.</p>}
      {items.length > 0 && (
        <div className="flex flex-col divide-y divide-black/5">
          {items.slice(0, 5).map((item) => (
            <Link
              key={item.productVariantId}
              to="/admin/inventory"
              className="flex items-center justify-between gap-3 py-2 text-sm hover:bg-luna-cream/50"
            >
              <span className="truncate">
                {item.productName} <span className="text-luna-charcoal/50">— {item.color} / {item.size}</span>
              </span>
              <span className={`shrink-0 text-xs font-semibold ${item.availableQuantity === 0 ? 'text-rose-600' : 'text-amber-600'}`}>
                {item.availableQuantity} en stock
              </span>
            </Link>
          ))}
        </div>
      )}
    </SectionCard>
  );
}

function TopProductsCard({ items }: { items: DashboardOverviewDto['topProducts'] }) {
  return (
    <SectionCard title="Meilleures ventes">
      {items.length === 0 && <p className="text-sm text-luna-charcoal/50">Aucune vente sur cette période.</p>}
      {items.length > 0 && (
        <div className="flex flex-col divide-y divide-black/5">
          {items.slice(0, 5).map((p) => (
            <div key={p.productId} className="flex items-center gap-3 py-2">
              <div className="h-10 w-10 shrink-0 overflow-hidden rounded bg-luna-cream">
                {p.imageUrl && <img src={p.imageUrl} alt="" className="h-full w-full object-cover" />}
              </div>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm">{p.productName}</p>
                <p className="text-xs text-luna-charcoal/60">
                  {p.unitsSold} unités · {p.orderCount} commandes
                </p>
              </div>
              <p className="shrink-0 text-sm font-medium tabular-nums">{formatPrice(p.revenue)}</p>
            </div>
          ))}
        </div>
      )}
    </SectionCard>
  );
}

function FunnelCard({ funnel }: { funnel: DashboardOverviewDto['funnel'] }) {
  const max = Math.max(1, ...funnel.map((f) => f.count));

  return (
    <SectionCard title="Entonnoir de conversion (commandes ayant atteint chaque étape durant la période)">
      <div className="flex flex-col gap-2.5">
        {funnel.map((stage) => (
          <div key={stage.key} className="flex items-center gap-3">
            <span className="w-32 shrink-0 truncate text-xs text-luna-charcoal/70">{stage.label}</span>
            <div className="h-3 flex-1 overflow-hidden rounded-full bg-luna-cream">
              <div
                className="h-full rounded-full bg-luna-accent transition-[width]"
                style={{ width: `${Math.max(3, (stage.count / max) * 100)}%` }}
              />
            </div>
            <span className="w-10 shrink-0 text-right text-xs font-medium tabular-nums">{stage.count}</span>
            <span className="w-12 shrink-0 text-right text-xs text-luna-charcoal/50 tabular-nums">
              {stage.conversionPercent !== null ? `${stage.conversionPercent}%` : ''}
            </span>
          </div>
        ))}
      </div>
    </SectionCard>
  );
}

function DeliveryCard({ delivery }: { delivery: DashboardOverviewDto['delivery'] }) {
  return (
    <SectionCard title="Performance de livraison">
      <div className="grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
        <div>
          <p className="text-xs text-luna-charcoal/60">Livrées</p>
          <p className="text-lg font-semibold">{delivery.delivered}</p>
        </div>
        <div>
          <p className="text-xs text-luna-charcoal/60">En livraison</p>
          <p className="text-lg font-semibold">{delivery.outForDelivery}</p>
        </div>
        <div>
          <p className="text-xs text-luna-charcoal/60">Refusées</p>
          <p className="text-lg font-semibold">{delivery.refused}</p>
        </div>
        <div>
          <p className="text-xs text-luna-charcoal/60">Retournées</p>
          <p className="text-lg font-semibold">{delivery.returned}</p>
        </div>
      </div>
      {delivery.byCarrier.length > 0 ? (
        <div className="mt-4 flex flex-col divide-y divide-black/5 border-t border-black/5 pt-2 text-sm">
          {delivery.byCarrier.map((c) => (
            <div key={c.carrier} className="flex items-center justify-between py-1.5">
              <span>{c.carrier}</span>
              <span className="text-xs text-luna-charcoal/60">
                {c.delivered} livrées · {c.returned} retours · {c.failed} échecs
              </span>
            </div>
          ))}
        </div>
      ) : (
        <p className="mt-4 border-t border-black/5 pt-3 text-xs text-luna-charcoal/40">
          Aucune expédition rattachée à un transporteur sur cette période.
        </p>
      )}
    </SectionCard>
  );
}

function ReturnsCard({ returns }: { returns: DashboardOverviewDto['returns'] }) {
  return (
    <SectionCard title="Retours">
      <p className="text-2xl font-semibold tabular-nums">{returns.totalReturned}</p>
      <p className="mb-3 text-xs text-luna-charcoal/60">commandes retournées sur la période</p>
      {returns.byReason.length > 0 ? (
        <div className="flex flex-col divide-y divide-black/5 border-t border-black/5 pt-2 text-sm">
          {returns.byReason.map((r) => (
            <div key={r.reason} className="flex items-center justify-between py-1.5">
              <span>{RETURN_REASON_LABELS[r.reason]}</span>
              <span className="font-medium">{r.count}</span>
            </div>
          ))}
        </div>
      ) : (
        <p className="border-t border-black/5 pt-3 text-xs text-luna-charcoal/40">Aucune cause de retour enregistrée.</p>
      )}
    </SectionCard>
  );
}

function MarketingCard({ marketing }: { marketing: DashboardOverviewDto['marketing'] }) {
  return (
    <SectionCard title="Sources marketing (UTM)">
      {marketing.length === 0 && <p className="text-sm text-luna-charcoal/50">Aucune commande sur cette période.</p>}
      {marketing.length > 0 && (
        <div className="flex flex-col divide-y divide-black/5">
          {marketing.map((m) => (
            <div key={m.source} className="flex items-center justify-between py-1.5 text-sm">
              <span className="truncate">{m.source}</span>
              <span className="shrink-0 text-xs text-luna-charcoal/60">
                {m.orderCount} commandes · {formatPrice(m.revenue)}
              </span>
            </div>
          ))}
        </div>
      )}
    </SectionCard>
  );
}

function RecentOrdersCard({ onSelect }: { onSelect: (orderId: string) => void }) {
  const { data, isLoading } = useQuery({
    queryKey: ['recent-orders'],
    queryFn: () => ordersApi.getPaged({ pageSize: 5 }),
  });

  return (
    <SectionCard title="Commandes récentes">
      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
      {data && data.items.length === 0 && <p className="text-sm text-luna-charcoal/60">Aucune commande.</p>}
      {data && data.items.length > 0 && (
        <div className="flex flex-col divide-y divide-black/5">
          {data.items.map((order) => (
            <button
              key={order.id}
              type="button"
              onClick={() => onSelect(order.id)}
              className="flex w-full items-center justify-between gap-3 py-2.5 text-left text-sm hover:bg-luna-cream/50"
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
    </SectionCard>
  );
}

export function DashboardPage() {
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const [presetKey, setPresetKey] = useState('thisMonth');
  const [customFrom, setCustomFrom] = useState(() => toApiDate(new Date()));
  const [customTo, setCustomTo] = useState(() => toApiDate(new Date()));

  const { from, to } = useMemo(() => {
    if (presetKey === 'custom') {
      return { from: customFrom, to: customTo };
    }
    const preset = DASHBOARD_PERIOD_PRESETS.find((p) => p.key === presetKey) ?? DASHBOARD_PERIOD_PRESETS[4];
    const range = preset.range();
    return { from: toApiDate(range.from), to: toApiDate(range.to) };
  }, [presetKey, customFrom, customTo]);

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['dashboard-overview', from, to],
    queryFn: () => dashboardApi.getOverview(from, to),
  });

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold">Tableau de bord</h1>
          <p className="text-xs text-luna-charcoal/50">{formatPeriodLabel(from, to)}</p>
        </div>
        <PeriodFilter
          presetKey={presetKey}
          customFrom={customFrom}
          customTo={customTo}
          onPresetChange={setPresetKey}
          onCustomChange={(f, t) => {
            setCustomFrom(f);
            setCustomTo(t);
          }}
        />
      </div>

      {isError && (
        <div className="mb-6 flex items-center justify-between rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          <span>Impossible de charger le tableau de bord.</span>
          <button type="button" onClick={() => refetch()} className="rounded-full bg-rose-600 px-4 py-1.5 text-xs font-medium text-white">
            Réessayer
          </button>
        </div>
      )}

      {isLoading && (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-24" />
          ))}
        </div>
      )}

      {data && (
        <div className={isFetching ? 'opacity-60 transition-opacity' : ''}>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            <KpiCard label="CA (livré)" kpi={data.kpis.revenue} hasComparisonData={data.hasComparisonData} formatValue={formatPrice} />
            <KpiCard
              label="Commandes créées"
              kpi={data.kpis.ordersCreated}
              hasComparisonData={data.hasComparisonData}
              formatValue={(v) => String(v)}
            />
            <KpiCard
              label="Confirmées"
              kpi={data.kpis.confirmed}
              hasComparisonData={data.hasComparisonData}
              formatValue={(v) => String(v)}
            />
            <KpiCard
              label="Livrées"
              kpi={data.kpis.delivered}
              hasComparisonData={data.hasComparisonData}
              formatValue={(v) => String(v)}
            />
            <KpiCard
              label="Retours"
              kpi={data.kpis.returned}
              hasComparisonData={data.hasComparisonData}
              formatValue={(v) => String(v)}
            />
            <KpiCard
              label="Annulées"
              kpi={data.kpis.cancelled}
              hasComparisonData={data.hasComparisonData}
              formatValue={(v) => String(v)}
            />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-4 lg:grid-cols-2">
            <SectionCard title={`Chiffre d'affaires (${data.revenueGranularity === 'hourly' ? 'par heure' : data.revenueGranularity === 'monthly' ? 'par mois' : 'par jour'})`}>
              <RevenueChart points={data.revenue} granularity={data.revenueGranularity} />
            </SectionCard>
            <SectionCard title="Commandes par statut (créées sur la période)">
              <StatusBarChart data={data.ordersByStatus} />
            </SectionCard>
          </div>

          <div className="mt-4">
            <FunnelCard funnel={data.funnel} />
          </div>

          <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
            <TopProductsCard items={data.topProducts} />
            <ActionQueueCard items={data.actionQueue} />
          </div>

          <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
            <DeliveryCard delivery={data.delivery} />
            <StockAlertsCard items={data.lowStock} />
          </div>

          <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
            <ReturnsCard returns={data.returns} />
            <MarketingCard marketing={data.marketing} />
          </div>
        </div>
      )}

      <div className="mt-4">
        <RecentOrdersCard onSelect={setSelectedOrderId} />
      </div>

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
    </div>
  );
}
