import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';

import { customersApi } from '../../lib/api/customers';
import { ordersApi } from '../../lib/api/orders';
import { formatPrice } from '../../lib/format/price';
import { ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';

const PAGE_SIZE = 20;

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('fr-FR');
}

// A customer is "at risk" once at least half of their orders ended up cancelled/refused/unreachable
// — worth flagging to an agent before they spend time confirming yet another order from this number.
function isAtRisk(totalOrders: number, cancelledOrRefused: number, unreachable: number) {
  return totalOrders >= 2 && (cancelledOrRefused + unreachable) / totalOrders >= 0.5;
}

function CustomerListView({ onView }: { onView: (phone: string) => void }) {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-customers', search, page],
    queryFn: () => customersApi.getPaged({ search: search || undefined, page, pageSize: PAGE_SIZE }),
  });

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-lg font-semibold">Clients {data ? `(${data.totalCount})` : ''}</h1>
        <input
          placeholder="Rechercher un nom ou un numéro..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          className="w-64 rounded border border-black/20 px-3 py-1.5 text-sm"
        />
      </div>

      <p className="mb-4 text-xs text-luna-charcoal/50">
        Il n'y a pas de compte client sur Luna (checkout invité) — chaque profil ci-dessous est
        reconstitué à partir des commandes passées avec ce numéro de téléphone.
      </p>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
      {isError && <p className="text-sm text-red-600">Impossible de charger les clients.</p>}
      {data && data.items.length === 0 && <p className="text-sm text-luna-charcoal/50">Aucun client trouvé.</p>}

      {data && data.items.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/10 text-xs text-luna-charcoal/60">
              <tr>
                <th className="px-4 py-2">Téléphone</th>
                <th className="px-4 py-2">Client</th>
                <th className="px-4 py-2">Wilaya</th>
                <th className="px-4 py-2">Commandes</th>
                <th className="px-4 py-2">Livrées</th>
                <th className="px-4 py-2">Annulées/Refusées</th>
                <th className="px-4 py-2">Injoignable</th>
                <th className="px-4 py-2">Total dépensé</th>
                <th className="px-4 py-2">Dernière commande</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((c) => {
                const risky = isAtRisk(c.totalOrders, c.cancelledOrRefusedOrders, c.unreachableCount);
                return (
                  <tr
                    key={c.phone}
                    onClick={() => onView(c.phone)}
                    className="cursor-pointer border-b border-black/5 last:border-0 hover:bg-luna-cream/40"
                  >
                    <td className="px-4 py-2 font-mono">{c.phone}</td>
                    <td className="px-4 py-2">
                      {c.fullName}
                      {risky && (
                        <span className="ms-2 rounded-full bg-red-100 px-2 py-0.5 text-[11px] text-red-700">
                          À risque
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-2">{c.wilaya}</td>
                    <td className="px-4 py-2">{c.totalOrders}</td>
                    <td className="px-4 py-2">{c.deliveredOrders}</td>
                    <td className="px-4 py-2">{c.cancelledOrRefusedOrders}</td>
                    <td className="px-4 py-2">{c.unreachableCount}</td>
                    <td className="px-4 py-2">{formatPrice(c.totalSpent)}</td>
                    <td className="px-4 py-2 text-luna-charcoal/60">{formatDate(c.lastOrderAtUtc)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {data && totalPages > 1 && (
        <div className="mt-3 flex items-center justify-between text-xs">
          <span className="text-luna-charcoal/60">Page {page} / {totalPages}</span>
          <div className="flex gap-2">
            <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)} className="rounded border border-black/20 px-2 py-1 disabled:opacity-40">
              Précédent
            </button>
            <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className="rounded border border-black/20 px-2 py-1 disabled:opacity-40">
              Suivant
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function CustomerDetailView({ phone, onBack }: { phone: string; onBack: () => void }) {
  const [page, setPage] = useState(1);

  const { data: summary } = useQuery({
    queryKey: ['admin-customers', phone, 'summary'],
    queryFn: () => customersApi.getPaged({ search: phone, page: 1, pageSize: 1 }),
  });
  const customer = summary?.items.find((c) => c.phone === phone);

  const { data: orders, isLoading } = useQuery({
    queryKey: ['admin-customer-orders', phone, page],
    queryFn: () => ordersApi.getPaged({ phone, page, pageSize: PAGE_SIZE }),
  });

  const totalPages = orders ? Math.max(1, Math.ceil(orders.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <button type="button" onClick={onBack} className="mb-4 flex items-center gap-1.5 text-sm text-luna-charcoal/60 hover:underline">
        <ArrowLeft className="h-4 w-4" /> Retour aux clients
      </button>

      <h1 className="text-lg font-semibold">{customer?.fullName ?? 'Client'}</h1>
      <p className="mb-4 font-mono text-sm text-luna-charcoal/60">{phone}</p>

      {customer && (
        <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-5">
          <div className="rounded-lg border border-black/10 bg-white p-3">
            <p className="text-xs text-luna-charcoal/50">Commandes</p>
            <p className="text-lg font-semibold">{customer.totalOrders}</p>
          </div>
          <div className="rounded-lg border border-black/10 bg-white p-3">
            <p className="text-xs text-luna-charcoal/50">Livrées</p>
            <p className="text-lg font-semibold">{customer.deliveredOrders}</p>
          </div>
          <div className="rounded-lg border border-black/10 bg-white p-3">
            <p className="text-xs text-luna-charcoal/50">Annulées/Refusées</p>
            <p className="text-lg font-semibold">{customer.cancelledOrRefusedOrders}</p>
          </div>
          <div className="rounded-lg border border-black/10 bg-white p-3">
            <p className="text-xs text-luna-charcoal/50">Injoignable</p>
            <p className="text-lg font-semibold">{customer.unreachableCount}</p>
          </div>
          <div className="rounded-lg border border-black/10 bg-white p-3">
            <p className="text-xs text-luna-charcoal/50">Total dépensé</p>
            <p className="text-lg font-semibold">{formatPrice(customer.totalSpent)}</p>
          </div>
        </div>
      )}

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

      {orders && orders.items.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/10 text-xs text-luna-charcoal/60">
              <tr>
                <th className="px-4 py-2">Commande</th>
                <th className="px-4 py-2">Date</th>
                <th className="px-4 py-2">Wilaya</th>
                <th className="px-4 py-2">Montant</th>
                <th className="px-4 py-2">Statut</th>
              </tr>
            </thead>
            <tbody>
              {orders.items.map((o) => (
                <tr key={o.id} className="border-b border-black/5 last:border-0">
                  <td className="px-4 py-2 font-mono">
                    <Link to={`/admin/orders/${o.id}`} className="hover:underline">{o.orderNumber}</Link>
                  </td>
                  <td className="px-4 py-2 text-luna-charcoal/60">{formatDate(o.createdAtUtc)}</td>
                  <td className="px-4 py-2">{o.wilaya}</td>
                  <td className="px-4 py-2">{formatPrice(o.total)}</td>
                  <td className="px-4 py-2">
                    <span className="rounded-full bg-luna-cream px-2 py-0.5 text-xs">{ORDER_STATUS_LABELS[o.status]}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {orders && totalPages > 1 && (
        <div className="mt-3 flex items-center justify-between text-xs">
          <span className="text-luna-charcoal/60">Page {page} / {totalPages}</span>
          <div className="flex gap-2">
            <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)} className="rounded border border-black/20 px-2 py-1 disabled:opacity-40">
              Précédent
            </button>
            <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className="rounded border border-black/20 px-2 py-1 disabled:opacity-40">
              Suivant
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export function CustomersPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const phone = searchParams.get('phone');

  return phone ? (
    <CustomerDetailView phone={phone} onBack={() => setSearchParams({})} />
  ) : (
    <CustomerListView onView={(p) => setSearchParams({ phone: p })} />
  );
}
