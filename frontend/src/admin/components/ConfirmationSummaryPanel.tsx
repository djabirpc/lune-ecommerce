import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Phone, PhoneOff, Clock, CheckCircle2, XCircle } from 'lucide-react';

import { ordersApi } from '../../lib/api/orders';
import { ApiError } from '../../lib/api/client';
import type { CallAttemptResult, OrderDetailDto } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';
import { CALL_ATTEMPT_RESULT_LABELS, DELIVERY_TYPE_LABELS, ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';
import { OrderActionsPanel } from './OrderActionsPanel';

const CALLABLE_STATUSES = new Set(['PendingConfirmation', 'CustomerUnreachable']);

// Confirmation-team-optimized view: everything needed to make the call and record its outcome in one
// place, without opening the full Order Detail page. Reuses IOrderCallAttemptService's existing
// business rules (only one NotAnswer actually transitions the order, Confirmed/Cancelled delegate to
// the same status-transition logic used everywhere else) via the already-implemented
// POST /api/orders/{id}/call-attempts — this view is new frontend wiring, not new backend logic.
export function ConfirmationSummaryPanel({ orderId, onClose }: { orderId: string; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [notes, setNotes] = useState('');
  const [showCallback, setShowCallback] = useState(false);
  const [nextCallAt, setNextCallAt] = useState('');
  const [callError, setCallError] = useState<string | null>(null);

  const { data: order, isLoading, isError } = useQuery({
    queryKey: ['admin-order', orderId],
    queryFn: () => ordersApi.getById(orderId),
  });

  const recordCall = useMutation({
    mutationFn: (vars: { result: CallAttemptResult; nextCallAtIso?: string | null }) =>
      ordersApi.recordCallAttempt(orderId, { result: vars.result, notes: notes.trim() || null, nextCallAt: vars.nextCallAtIso ?? null }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-order', orderId], updated);
      queryClient.invalidateQueries({ queryKey: ['admin-orders-confirmation'] });
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      setNotes('');
      setShowCallback(false);
      setNextCallAt('');
      setCallError(null);
    },
    onError: (err) => setCallError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  function handleOrderChanged(updated: OrderDetailDto) {
    queryClient.setQueryData(['admin-order', orderId], updated);
    queryClient.invalidateQueries({ queryKey: ['admin-orders-confirmation'] });
    queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
  }

  function submitCallback() {
    if (!nextCallAt) return;
    recordCall.mutate({ result: 'CallbackScheduled', nextCallAtIso: new Date(nextCallAt).toISOString() });
  }

  if (isLoading) {
    return <p className="text-sm text-luna-charcoal/60">Chargement...</p>;
  }

  if (isError || !order) {
    return <p className="text-sm text-red-600">Impossible de charger cette commande.</p>;
  }

  const isCallable = CALLABLE_STATUSES.has(order.status);

  return (
    <div className="flex flex-col gap-5">
      <div>
        <div className="flex items-center justify-between gap-2">
          <span className="font-mono text-sm font-medium">{order.orderNumber}</span>
          <span className="rounded-full bg-luna-rose px-2.5 py-1 text-xs font-medium text-luna-accent-dark">
            {ORDER_STATUS_LABELS[order.status]}
          </span>
        </div>
        <p className="mt-1 text-lg font-semibold text-luna-black">
          {order.firstName} {order.lastName}
        </p>
        <a
          href={`tel:${order.phone}`}
          className="mt-2 inline-flex items-center gap-2 rounded-full border border-luna-black px-4 py-2 text-sm font-medium"
        >
          <Phone className="h-4 w-4" /> Appeler le client — {order.phone}
        </a>
      </div>

      <div className="rounded-lg border border-black/10 bg-white p-4">
        <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Commande</h3>
        <div className="flex flex-col divide-y divide-black/5">
          {order.items.map((item) => (
            <div key={item.id} className="flex items-center justify-between gap-3 py-1.5 text-sm">
              <span className="min-w-0 truncate">
                {item.productName} — {item.color}/{item.size} × {item.quantity}
              </span>
              <span className="shrink-0 font-medium">{formatPrice(item.lineTotal)}</span>
            </div>
          ))}
        </div>
        <div className="mt-2 flex items-center justify-between border-t border-black/10 pt-2 text-sm font-medium">
          <span>Total</span>
          <span>{formatPrice(order.total)}</span>
        </div>
      </div>

      <div className="rounded-lg border border-black/10 bg-white p-4 text-sm">
        <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Livraison</h3>
        <p>Wilaya : {order.wilaya}</p>
        <p>Commune : {order.commune}</p>
        {order.address && <p>Adresse : {order.address}</p>}
        <p className="text-luna-charcoal/60">{DELIVERY_TYPE_LABELS[order.deliveryType]}</p>
        {order.notes && <p className="mt-1 text-xs text-luna-charcoal/60">Remarque : {order.notes}</p>}
      </div>

      <div className="rounded-lg border border-black/10 bg-white p-4">
        <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Historique des appels</h3>
        {order.callAttempts.length === 0 ? (
          <p className="text-sm text-luna-charcoal/60">Aucun appel enregistré pour le moment.</p>
        ) : (
          <div className="flex flex-col divide-y divide-black/5 text-sm">
            {order.callAttempts.map((a) => (
              <div key={a.id} className="py-1.5">
                <p>
                  Appel #{a.attemptNumber} — {CALL_ATTEMPT_RESULT_LABELS[a.result]}
                  <span className="ml-2 text-xs text-luna-charcoal/60">
                    {new Date(a.calledAtUtc).toLocaleString('fr-FR')}
                    {a.agentUserName ? ` · ${a.agentUserName}` : ''}
                  </span>
                </p>
                {a.notes && <p className="text-xs text-luna-charcoal/70">{a.notes}</p>}
                {a.nextCallAtUtc && (
                  <p className="text-xs text-luna-charcoal/70">Prochain rappel : {new Date(a.nextCallAtUtc).toLocaleString('fr-FR')}</p>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {isCallable ? (
        <div>
          <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Résultat de l'appel</h3>
          <label className="mb-2 flex flex-col gap-1 text-sm">
            Note (optionnel)
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={2}
              className="rounded border border-black/20 bg-white px-2 py-1.5 text-sm"
            />
          </label>

          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              disabled={recordCall.isPending}
              onClick={() => recordCall.mutate({ result: 'Confirmed' })}
              className="flex items-center justify-center gap-1.5 rounded-full bg-luna-black px-3 py-2 text-sm text-white disabled:opacity-40"
            >
              <CheckCircle2 className="h-4 w-4" /> Confirmer
            </button>
            <button
              type="button"
              disabled={recordCall.isPending}
              onClick={() => recordCall.mutate({ result: 'Cancelled' })}
              className="flex items-center justify-center gap-1.5 rounded-full border border-luna-black px-3 py-2 text-sm disabled:opacity-40"
            >
              <XCircle className="h-4 w-4" /> Annuler
            </button>
            <button
              type="button"
              disabled={recordCall.isPending}
              onClick={() => setShowCallback((v) => !v)}
              className="flex items-center justify-center gap-1.5 rounded-full border border-luna-black px-3 py-2 text-sm disabled:opacity-40"
            >
              <Clock className="h-4 w-4" /> Rappel plus tard
            </button>
            <button
              type="button"
              disabled={recordCall.isPending}
              onClick={() => recordCall.mutate({ result: 'NoAnswer' })}
              className="flex items-center justify-center gap-1.5 rounded-full border border-luna-black px-3 py-2 text-sm disabled:opacity-40"
            >
              <PhoneOff className="h-4 w-4" /> Pas de réponse
            </button>
          </div>

          {showCallback && (
            <div className="mt-2 flex items-center gap-2">
              <input
                type="datetime-local"
                value={nextCallAt}
                onChange={(e) => setNextCallAt(e.target.value)}
                className="flex-1 rounded border border-black/20 bg-white px-2 py-1.5 text-sm"
              />
              <button
                type="button"
                disabled={!nextCallAt || recordCall.isPending}
                onClick={submitCallback}
                className="rounded-full bg-luna-black px-3 py-1.5 text-xs text-white disabled:opacity-40"
              >
                Programmer
              </button>
            </div>
          )}

          {callError && <p className="mt-2 text-sm text-red-600">{callError}</p>}
        </div>
      ) : (
        <div>
          <h3 className="mb-2 text-xs font-semibold uppercase text-luna-charcoal/60">Actions</h3>
          <OrderActionsPanel order={order} onChanged={handleOrderChanged} />
        </div>
      )}

      <Link
        to={`/admin/orders/${order.id}`}
        onClick={onClose}
        className="rounded-full border border-luna-black px-4 py-2.5 text-center text-sm font-medium hover:bg-luna-cream"
      >
        Voir tous les détails
      </Link>
    </div>
  );
}
