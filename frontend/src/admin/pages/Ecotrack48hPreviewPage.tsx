import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Banknote, CheckCircle2, Circle, Loader2, MapPin, Package, Printer, RotateCcw, Search, Truck, X } from 'lucide-react';

import { ecotrackPreviewApi, type EcotrackPreviewSoftFilter } from '../../lib/api/ecotrackPreview';
import { ApiError } from '../../lib/api/client';
import { formatPrice } from '../../lib/format/price';
import { AlgeriaMap } from '../components/AlgeriaMap';

function prettifyStatus(status: string): string {
  return status.replaceAll('_', ' ').replace(/^./, (c) => c.toUpperCase());
}

function formatDate(iso: string | null): string {
  return iso ? new Date(iso).toLocaleString('fr-FR') : '—';
}

// Local, single-use debounce — text/number filter inputs shouldn't refetch on every keystroke, but
// date inputs (native pickers, no continuous typing) apply immediately without going through this.
function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}

function StatCard({ icon, label, value, tone }: { icon: React.ReactNode; label: string; value: string; tone: string }) {
  return (
    <div className="rounded-lg border border-black/10 bg-white p-4">
      <div className={`flex items-center gap-2 ${tone}`}>
        {icon}
        <p className="text-xs uppercase text-luna-charcoal/60">{label}</p>
      </div>
      <p className="mt-1 text-2xl font-semibold">{value}</p>
    </div>
  );
}

function DashboardView({
  onSelectTracking,
}: {
  onSelectTracking: (tracking: string) => void;
}) {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [wilayaId, setWilayaId] = useState<number | null>(null);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [searchDraft, setSearchDraft] = useState('');
  const [minAmountDraft, setMinAmountDraft] = useState('');
  const [maxAmountDraft, setMaxAmountDraft] = useState('');

  const search = useDebouncedValue(searchDraft, 350);
  const minAmount = useDebouncedValue(minAmountDraft, 350);
  const maxAmount = useDebouncedValue(maxAmountDraft, 350);

  // "Soft" filters (period/amount/search) narrow both the dashboard (map/stat cards/top-wilayas) and
  // the table below — status/wilayaId ("hard" filters) narrow only the table, see
  // Ecotrack48hPreviewFilter's doc comment on the backend for why.
  const softFilter: EcotrackPreviewSoftFilter = {
    startDate: startDate || undefined,
    endDate: endDate || undefined,
    minAmount: minAmount ? Number(minAmount) : undefined,
    maxAmount: maxAmount ? Number(maxAmount) : undefined,
    search: search || undefined,
  };

  useEffect(() => {
    setPage(1);
  }, [startDate, endDate, minAmount, maxAmount, search]);

  const dashboard = useQuery({
    queryKey: ['ecotrack-preview-dashboard', softFilter],
    queryFn: () => ecotrackPreviewApi.getDashboard(softFilter),
  });

  const orders = useQuery({
    queryKey: ['ecotrack-preview-orders', page, status, wilayaId, softFilter],
    queryFn: () => ecotrackPreviewApi.getOrders({ page, status: status || undefined, wilayaId: wilayaId ?? undefined, ...softFilter }),
  });

  const counts = Object.fromEntries((dashboard.data?.byWilaya ?? []).map((w) => [w.wilayaId, w.count]));
  const hasActiveFilter = !!(status || wilayaId || startDate || endDate || searchDraft || minAmountDraft || maxAmountDraft);

  function selectWilaya(id: number | null) {
    setWilayaId(id);
    setPage(1);
  }

  function resetFilters() {
    setStatus('');
    setWilayaId(null);
    setStartDate('');
    setEndDate('');
    setSearchDraft('');
    setMinAmountDraft('');
    setMaxAmountDraft('');
    setPage(1);
  }

  return (
    <div>
      {dashboard.data && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
          <StatCard icon={<Package className="h-4 w-4" />} label="Total colis" value={String(dashboard.data.totalOrders)} tone="text-luna-charcoal/60" />
          <StatCard
            icon={<Banknote className="h-4 w-4" />}
            label="Montant total"
            value={formatPrice(dashboard.data.totalAmount)}
            tone="text-luna-charcoal/60"
          />
          <StatCard icon={<CheckCircle2 className="h-4 w-4" />} label="Livrés" value={String(dashboard.data.deliveredCount)} tone="text-green-700" />
          <StatCard icon={<Truck className="h-4 w-4" />} label="En cours" value={String(dashboard.data.inProcessCount)} tone="text-luna-accent-dark" />
          <StatCard icon={<RotateCcw className="h-4 w-4" />} label="Retours" value={String(dashboard.data.returnCount)} tone="text-red-600" />
        </div>
      )}

      <div className="mt-6 grid gap-4 lg:grid-cols-[1.1fr_0.9fr]">
        <div className="rounded-lg border border-black/10 bg-white p-4">
          <div className="mb-2 flex items-center justify-between">
            <h2 className="flex items-center gap-1.5 text-sm font-semibold uppercase text-luna-charcoal/60">
              <MapPin className="h-4 w-4" /> Carte des wilayas
            </h2>
            {wilayaId && (
              <button type="button" onClick={() => selectWilaya(null)} className="flex items-center gap-1 text-xs text-luna-accent-dark underline">
                <X className="h-3 w-3" /> Effacer la sélection
              </button>
            )}
          </div>
          <p className="mb-2 text-xs text-luna-charcoal/50">Cliquez une wilaya pour filtrer le tableau ci-dessous.</p>
          {dashboard.isLoading ? (
            <p className="text-sm text-luna-charcoal/60">Chargement...</p>
          ) : (
            <AlgeriaMap counts={counts} selectedWilayaId={wilayaId} onSelectWilaya={selectWilaya} />
          )}
        </div>

        <div className="rounded-lg border border-black/10 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Wilayas les plus livrées</h2>
          {dashboard.data && dashboard.data.byWilaya.length > 0 ? (
            <div className="flex flex-col gap-2">
              {dashboard.data.byWilaya.slice(0, 10).map((w, i) => {
                const max = dashboard.data!.byWilaya[0].count;
                const pct = Math.max(6, Math.round((w.count / max) * 100));
                const isSelected = wilayaId === w.wilayaId;
                return (
                  <button
                    key={w.wilayaId}
                    type="button"
                    onClick={() => selectWilaya(isSelected ? null : w.wilayaId)}
                    className={`rounded px-2 py-1.5 text-left text-sm hover:bg-luna-cream/60 ${isSelected ? 'bg-luna-rose' : ''}`}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="flex items-center gap-1.5">
                        <span className="w-4 text-xs text-luna-charcoal/40">{i + 1}</span>
                        {w.wilayaName}
                      </span>
                      <span className="shrink-0 text-xs text-luna-charcoal/60">{w.count} colis</span>
                    </div>
                    <div className="mt-1 h-1.5 w-full rounded-full bg-luna-cream">
                      <div className="h-1.5 rounded-full bg-luna-accent" style={{ width: `${pct}%` }} />
                    </div>
                  </button>
                );
              })}
            </div>
          ) : (
            <p className="text-sm text-luna-charcoal/60">Chargement...</p>
          )}
        </div>
      </div>

      <div className="mt-6">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-sm font-semibold uppercase text-luna-charcoal/60">Colis</h2>
          {hasActiveFilter && (
            <button type="button" onClick={resetFilters} className="flex items-center gap-1 text-xs text-luna-accent-dark underline">
              <X className="h-3 w-3" /> Réinitialiser les filtres
            </button>
          )}
        </div>

        <div className="mb-4 grid gap-3 rounded-lg border border-black/10 bg-white p-4 sm:grid-cols-2 lg:grid-cols-4">
          <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
            Statut
            <select
              value={status}
              onChange={(e) => {
                setStatus(e.target.value);
                setPage(1);
              }}
              className="rounded border border-black/20 px-3 py-1.5 text-sm text-luna-black"
            >
              <option value="">Tous les statuts</option>
              {(dashboard.data?.byStatus ?? []).map((s) => (
                <option key={s.status} value={s.status}>
                  {prettifyStatus(s.status)} ({s.count})
                </option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
            Wilaya
            <select
              value={wilayaId ?? ''}
              onChange={(e) => selectWilaya(e.target.value ? Number(e.target.value) : null)}
              className="rounded border border-black/20 px-3 py-1.5 text-sm text-luna-black"
            >
              <option value="">Toutes les wilayas</option>
              {(dashboard.data?.byWilaya ?? [])
                .slice()
                .sort((a, b) => a.wilayaName.localeCompare(b.wilayaName))
                .map((w) => (
                  <option key={w.wilayaId} value={w.wilayaId}>
                    {w.wilayaName} ({w.count})
                  </option>
                ))}
            </select>
          </label>

          <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
            Période — du
            <input
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              className="rounded border border-black/20 px-3 py-1.5 text-sm text-luna-black"
            />
          </label>
          <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
            Période — au
            <input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              className="rounded border border-black/20 px-3 py-1.5 text-sm text-luna-black"
            />
          </label>

          <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
            Montant à récupérer — min (DA)
            <input
              type="number"
              min={0}
              value={minAmountDraft}
              onChange={(e) => setMinAmountDraft(e.target.value)}
              placeholder="ex. 1000"
              className="rounded border border-black/20 px-3 py-1.5 text-sm text-luna-black"
            />
          </label>
          <label className="flex flex-col gap-1 text-xs text-luna-charcoal/60">
            Montant à récupérer — max (DA)
            <input
              type="number"
              min={0}
              value={maxAmountDraft}
              onChange={(e) => setMaxAmountDraft(e.target.value)}
              placeholder="ex. 6000"
              className="rounded border border-black/20 px-3 py-1.5 text-sm text-luna-black"
            />
          </label>

          <label className="col-span-2 flex flex-col gap-1 text-xs text-luna-charcoal/60 lg:col-span-2">
            Recherche (tracking, client, téléphone, référence)
            <span className="relative">
              <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-luna-charcoal/40" />
              <input
                value={searchDraft}
                onChange={(e) => setSearchDraft(e.target.value)}
                placeholder="ex. 0555... ou EC9BF..."
                className="w-full rounded border border-black/20 py-1.5 pl-8 pr-3 text-sm text-luna-black"
              />
            </span>
          </label>
        </div>

        {orders.isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
        {orders.isError && (
          <p className="text-sm text-red-600">
            {orders.error instanceof ApiError ? orders.error.message : 'Impossible de charger les colis.'}
          </p>
        )}

        {orders.data && (
          <>
            <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-black/10 text-xs uppercase text-luna-charcoal/60">
                  <tr>
                    <th className="px-4 py-2">Tracking</th>
                    <th className="px-4 py-2">Client</th>
                    <th className="px-4 py-2">Wilaya</th>
                    <th className="px-4 py-2">Produits</th>
                    <th className="px-4 py-2">Montant</th>
                    <th className="px-4 py-2">Statut</th>
                    <th className="px-4 py-2">Créé le</th>
                  </tr>
                </thead>
                <tbody>
                  {orders.data.items.map((o) => (
                    <tr
                      key={o.tracking}
                      onClick={() => onSelectTracking(o.tracking)}
                      className="cursor-pointer border-b border-black/5 last:border-0 hover:bg-luna-cream/50"
                    >
                      <td className="px-4 py-2 font-mono text-xs">{o.tracking}</td>
                      <td className="px-4 py-2">{o.client}</td>
                      <td className="px-4 py-2">{o.wilayaName ?? '—'}</td>
                      <td className="max-w-[200px] truncate px-4 py-2">{o.products ?? '—'}</td>
                      <td className="px-4 py-2">{o.amount !== null ? formatPrice(o.amount) : '—'}</td>
                      <td className="px-4 py-2">
                        <span className="rounded-full bg-luna-cream px-2 py-1 text-xs">{prettifyStatus(o.status)}</span>
                      </td>
                      <td className="px-4 py-2 text-xs text-luna-charcoal/60">{formatDate(o.createdAtUtc)}</td>
                    </tr>
                  ))}
                  {orders.data.items.length === 0 && (
                    <tr>
                      <td colSpan={7} className="px-4 py-8 text-center text-luna-charcoal/60">
                        Aucun colis pour ces filtres.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            <div className="mt-4 flex items-center justify-between text-sm">
              <span className="text-luna-charcoal/60">
                Page {orders.data.currentPage} / {orders.data.lastPage} ({orders.data.total} colis)
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
                  disabled={page >= orders.data.lastPage}
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
    </div>
  );
}

function ClientView({ tracking, onTrackingChange }: { tracking: string; onTrackingChange: (tracking: string) => void }) {
  const [draft, setDraft] = useState(tracking);
  const [labelState, setLabelState] = useState<'idle' | 'loading' | 'error'>('idle');

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['ecotrack-preview-tracking', tracking],
    queryFn: () => ecotrackPreviewApi.getTracking(tracking),
    enabled: !!tracking,
  });

  async function handleDownloadLabel() {
    setLabelState('loading');
    try {
      const blob = await ecotrackPreviewApi.getLabelPdf(tracking);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${tracking}.pdf`;
      link.click();
      URL.revokeObjectURL(url);
      setLabelState('idle');
    } catch {
      setLabelState('error');
    }
  }

  return (
    <div>
      <p className="mb-3 text-sm text-luna-charcoal/60">
        Aperçu de ce qu'une page de suivi client pourrait afficher pour un colis donné (données réelles depuis 48h
        Express).
      </p>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          onTrackingChange(draft.trim());
        }}
        className="mb-4 flex gap-2 sm:max-w-md"
      >
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Numéro de tracking, ex. EC9BF62609172240265"
          className="flex-1 rounded border border-black/20 px-3 py-2 text-sm"
        />
        <button type="submit" className="rounded-full bg-luna-black px-4 py-2 text-sm text-white">
          Voir
        </button>
      </form>

      {!tracking && <p className="text-sm text-luna-charcoal/60">Colle un numéro de tracking, ou choisis un colis depuis le tableau de bord.</p>}
      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
      {isError && (
        <p className="text-sm text-red-600">{error instanceof ApiError ? error.message : "Impossible de charger ce colis."}</p>
      )}

      {data && (
        <div className="max-w-md rounded-lg border border-black/10 bg-white p-5">
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2 text-luna-black">
              <Package className="h-5 w-5" />
              <span className="font-mono text-sm">{data.tracking}</span>
            </div>
            <button
              type="button"
              onClick={handleDownloadLabel}
              disabled={labelState === 'loading'}
              className="flex shrink-0 items-center gap-1.5 rounded-full border border-black/20 px-3 py-1.5 text-xs font-medium text-luna-black hover:bg-luna-cream disabled:opacity-50"
            >
              {labelState === 'loading' ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Printer className="h-3.5 w-3.5" />}
              Étiquette PDF
            </button>
          </div>
          {labelState === 'error' && (
            <p className="mt-2 text-xs text-red-600">Impossible de récupérer l'étiquette pour ce colis.</p>
          )}
          {data.recipientName && <p className="mt-2 text-lg font-semibold">{data.recipientName}</p>}
          {data.currentStation && (
            <p className="mt-1 flex items-center gap-1.5 text-sm text-luna-charcoal/70">
              <Truck className="h-3.5 w-3.5" /> Station actuelle : {data.currentStation}
            </p>
          )}

          {data.activity.length > 0 ? (
            <ol className="mt-5 space-y-4 border-t border-black/10 pt-4">
              {data.activity.map((event, index) => {
                const isLast = index === data.activity.length - 1;
                return (
                  <li key={index} className="flex gap-3">
                    <span
                      className={`mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full border ${
                        isLast ? 'border-luna-accent bg-luna-accent text-white' : 'border-black/15 text-luna-charcoal/50'
                      }`}
                    >
                      {isLast ? <CheckCircle2 className="h-3.5 w-3.5" /> : <Circle className="h-2 w-2" />}
                    </span>
                    <div>
                      <p className="text-sm text-luna-black">{event.status ? prettifyStatus(event.status) : 'Mise à jour'}</p>
                      <p className="text-xs text-luna-charcoal/60">
                        {formatDate(event.occurredAtUtc)}
                        {event.station ? ` · ${event.station}` : ''}
                      </p>
                    </div>
                  </li>
                );
              })}
            </ol>
          ) : (
            <p className="mt-4 text-sm text-luna-charcoal/60">Aucun évènement de suivi pour ce colis pour le moment.</p>
          )}
        </div>
      )}
    </div>
  );
}

export function Ecotrack48hPreviewPage() {
  const [tab, setTab] = useState<'dashboard' | 'client'>('dashboard');
  const [selectedTracking, setSelectedTracking] = useState('');

  return (
    <div>
      <h1 className="text-xl font-semibold">Aperçu 48h Express</h1>
      <div className="mt-2 rounded-lg border border-luna-accent/30 bg-luna-rose/30 px-4 py-3 text-sm text-luna-accent-dark">
        Page de démonstration, lecture seule. Elle interroge directement ton compte 48h Express réel mais ne crée, ne
        modifie et ne supprime jamais rien — et n'est reliée à aucune commande ou page du site Luna.
      </div>

      <div className="mt-4 flex gap-2 border-b border-black/10">
        <button
          type="button"
          onClick={() => setTab('dashboard')}
          className={`px-4 py-2 text-sm font-medium ${tab === 'dashboard' ? 'border-b-2 border-luna-black text-luna-black' : 'text-luna-charcoal/60'}`}
        >
          Tableau de bord
        </button>
        <button
          type="button"
          onClick={() => setTab('client')}
          className={`px-4 py-2 text-sm font-medium ${tab === 'client' ? 'border-b-2 border-luna-black text-luna-black' : 'text-luna-charcoal/60'}`}
        >
          Vue client
        </button>
      </div>

      <div className="mt-4">
        {tab === 'dashboard' ? (
          <DashboardView
            onSelectTracking={(tracking) => {
              setSelectedTracking(tracking);
              setTab('client');
            }}
          />
        ) : (
          <ClientView tracking={selectedTracking} onTrackingChange={setSelectedTracking} />
        )}
      </div>
    </div>
  );
}
