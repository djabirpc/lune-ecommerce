import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Clock, PhoneOff, Phone, ChevronRight, Inbox } from 'lucide-react';

import { ordersApi } from '../../lib/api/orders';
import type { OrderStatus, OrderSummaryDto, PagedResult } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';

const PAGE_SIZE = 50;

function OrderQueue({
  title,
  icon,
  tone,
  emptyMessage,
  data,
  isLoading,
  isError,
}: {
  title: string;
  icon: React.ReactNode;
  tone: string;
  emptyMessage: string;
  data: PagedResult<OrderSummaryDto> | undefined;
  isLoading: boolean;
  isError: boolean;
}) {
  return (
    <div className="rounded-lg border border-black/10 bg-white">
      <div className="flex items-center justify-between border-b border-black/10 px-4 py-3">
        <div className={`flex items-center gap-2 ${tone}`}>
          {icon}
          <h2 className="text-sm font-semibold text-luna-black">{title}</h2>
        </div>
        {data && (
          <span className="rounded-full bg-luna-cream px-2.5 py-1 text-xs font-medium text-luna-charcoal/70">
            {data.totalCount}
          </span>
        )}
      </div>

      {isLoading && (
        <div className="flex flex-col gap-2 p-4">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-12 animate-pulse rounded bg-luna-cream/60" />
          ))}
        </div>
      )}

      {isError && <p className="p-4 text-sm text-red-600">Impossible de charger les commandes.</p>}

      {data && data.items.length === 0 && (
        <div className="flex flex-col items-center gap-2 py-10 text-center text-sm text-luna-charcoal/50">
          <Inbox className="h-6 w-6" />
          <p>{emptyMessage}</p>
        </div>
      )}

      {data && data.items.length > 0 && (
        <div className="divide-y divide-black/5">
          {data.items.map((order) => (
            <div key={order.id} className="flex flex-wrap items-center gap-x-3 gap-y-2 px-4 py-3 hover:bg-luna-cream/40">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-x-2">
                  <Link to={`/admin/orders/${order.id}`} className="font-mono text-xs font-medium underline">
                    {order.orderNumber}
                  </Link>
                  <span className="text-xs text-luna-charcoal/50">{new Date(order.createdAtUtc).toLocaleString('fr-FR')}</span>
                </div>
                <p className="mt-0.5 truncate text-sm text-luna-black">
                  {order.customerFullName} · {order.wilaya}
                </p>
              </div>
              <span className="shrink-0 text-sm font-medium text-luna-black">{formatPrice(order.total)}</span>
              <a
                href={`tel:${order.phone}`}
                className="flex shrink-0 items-center gap-1.5 rounded-full border border-luna-black px-3 py-1.5 text-xs text-luna-black"
              >
                <Phone className="h-3.5 w-3.5" /> {order.phone}
              </a>
              <Link
                to={`/admin/orders/${order.id}`}
                aria-label="Voir la commande"
                className="shrink-0 rounded-full p-1.5 text-luna-charcoal/50 hover:bg-luna-cream hover:text-luna-black"
              >
                <ChevronRight className="h-4 w-4" />
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function useQueueQuery(status: OrderStatus) {
  return useQuery({
    queryKey: ['admin-orders-confirmation', status],
    queryFn: () => ordersApi.getPaged({ status, page: 1, pageSize: PAGE_SIZE }),
  });
}

export function OrderConfirmationCenterPage() {
  const pending = useQueueQuery('PendingConfirmation');
  const unreachable = useQueueQuery('CustomerUnreachable');

  return (
    <div>
      <h1 className="text-xl font-semibold">Centre de confirmation</h1>
      <p className="mt-1 text-sm text-luna-charcoal/60">Appelez les clients pour confirmer leurs commandes avant préparation.</p>

      <div className="mt-6 grid grid-cols-2 gap-4 sm:max-w-md">
        <div className="rounded-lg border border-black/10 bg-white p-4">
          <div className="flex items-center gap-2 text-luna-charcoal/60">
            <Clock className="h-4 w-4" />
            <p className="text-xs uppercase">En attente</p>
          </div>
          <p className="mt-1 text-2xl font-semibold">{pending.isLoading ? '…' : (pending.data?.totalCount ?? 0)}</p>
        </div>
        <div className="rounded-lg border border-black/10 bg-white p-4">
          <div className="flex items-center gap-2 text-luna-accent-dark">
            <PhoneOff className="h-4 w-4" />
            <p className="text-xs uppercase">Injoignables</p>
          </div>
          <p className="mt-1 text-2xl font-semibold">{unreachable.isLoading ? '…' : (unreachable.data?.totalCount ?? 0)}</p>
        </div>
      </div>

      <div className="mt-6 flex flex-col gap-6">
        <OrderQueue
          title="En attente de confirmation"
          icon={<Clock className="h-4 w-4" />}
          tone="text-luna-charcoal/60"
          emptyMessage="Aucune commande en attente de confirmation."
          data={pending.data}
          isLoading={pending.isLoading}
          isError={pending.isError}
        />
        <OrderQueue
          title="Injoignables"
          icon={<PhoneOff className="h-4 w-4" />}
          tone="text-luna-accent-dark"
          emptyMessage="Aucun client injoignable pour le moment."
          data={unreachable.data}
          isLoading={unreachable.isLoading}
          isError={unreachable.isError}
        />
      </div>
    </div>
  );
}
