import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { ApiError } from '../../lib/api/client';
import type { CallAttemptResult, OrderStatus } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';
import { CALL_ATTEMPT_RESULT_LABELS, DELIVERY_TYPE_LABELS, ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';
import { ALLOWED_TRANSITIONS, ORDER_ACTION_LABELS, requiresReason } from '../../lib/orders/transitions';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

const CALLABLE_STATUSES: OrderStatus[] = ['PendingConfirmation', 'CustomerUnreachable'];
const CALL_ATTEMPT_RESULTS: CallAttemptResult[] = ['NoAnswer', 'Confirmed', 'Cancelled', 'CallbackScheduled'];

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState<string | null>(null);
  const [callResult, setCallResult] = useState<CallAttemptResult>('NoAnswer');
  const [callNotes, setCallNotes] = useState('');
  const [nextCallAt, setNextCallAt] = useState('');
  const [callError, setCallError] = useState<string | null>(null);

  const { data: order, isLoading, isError } = useQuery({
    queryKey: ['admin-order', id],
    queryFn: () => ordersApi.getById(id!),
    enabled: !!id,
  });

  const changeStatus = useMutation({
    mutationFn: (vars: { newStatus: OrderStatus; reason: string | null }) => ordersApi.changeStatus(id!, vars),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-order', id], updated);
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      setActionError(null);
    },
    onError: (err) => setActionError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const recordCallAttempt = useMutation({
    mutationFn: () =>
      ordersApi.recordCallAttempt(id!, {
        result: callResult,
        notes: callNotes.trim() || null,
        nextCallAt: callResult === 'CallbackScheduled' && nextCallAt ? new Date(nextCallAt).toISOString() : null,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-order', id], updated);
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      setCallNotes('');
      setNextCallAt('');
      setCallResult('NoAnswer');
      setCallError(null);
    },
    onError: (err) => setCallError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  if (isLoading) {
    return <p className="text-sm text-luna-charcoal/60">Chargement...</p>;
  }

  if (isError || !order) {
    return <PagePlaceholder title="Commande introuvable" />;
  }

  const nextStatuses = ALLOWED_TRANSITIONS[order.status];

  function handleTransition(newStatus: OrderStatus) {
    let reason: string | null = null;
    if (requiresReason(newStatus)) {
      reason = window.prompt(`Raison pour "${ORDER_ACTION_LABELS[newStatus]}" :`);
      if (reason === null) return;
    }
    changeStatus.mutate({ newStatus, reason: reason || null });
  }

  return (
    <div>
      <Link to="/admin/orders" className="mb-4 inline-block text-sm underline">
        ← Retour aux commandes
      </Link>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold">{order.orderNumber}</h1>
        <span className="rounded-full bg-luna-cream px-3 py-1 text-sm font-medium">
          {ORDER_STATUS_LABELS[order.status]}
        </span>
      </div>

      <div className="mt-6 grid gap-6 sm:grid-cols-2">
        <div>
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Client</h2>
          <p className="text-sm">
            {order.firstName} {order.lastName}
          </p>
          <p className="text-sm">{order.phone}</p>
          <p className="text-sm">
            {order.address}, {order.commune}, {order.wilaya}
          </p>
          <p className="text-sm">{DELIVERY_TYPE_LABELS[order.deliveryType]}</p>
          {order.notes && <p className="mt-1 text-sm text-luna-charcoal/70">Note : {order.notes}</p>}
        </div>

        <div>
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Paiement</h2>
          <p className="text-sm">{order.paymentMethod} — {order.paymentStatus}</p>
          <p className="text-sm">Sous-total : {formatPrice(order.subtotal)}</p>
          {order.discountTotal > 0 && (
            <p className="text-sm text-green-700">Réduction : −{formatPrice(order.discountTotal)}</p>
          )}
          <p className="text-sm">Livraison : {formatPrice(order.shippingCost)}</p>
          <p className="text-sm font-medium">Total : {formatPrice(order.total)}</p>
        </div>
      </div>

      <div className="mt-6">
        <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Articles</h2>
        <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white">
          {order.items.map((item) => (
            <div key={item.id} className="flex items-center justify-between px-4 py-2 text-sm">
              <span>
                {item.productName} ({item.color}/{item.size}) × {item.quantity}
              </span>
              <span>{formatPrice(item.lineTotal)}</span>
            </div>
          ))}
        </div>
      </div>

      {nextStatuses.length > 0 && (
        <div className="mt-6">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Actions</h2>
          <div className="flex flex-wrap gap-2">
            {nextStatuses.map((next) => (
              <button
                key={next}
                type="button"
                disabled={changeStatus.isPending}
                onClick={() => handleTransition(next)}
                className="rounded-full border border-luna-black px-4 py-2 text-sm disabled:opacity-40"
              >
                {ORDER_ACTION_LABELS[next]}
              </button>
            ))}
          </div>
          {actionError && <p className="mt-2 text-sm text-red-600">{actionError}</p>}
        </div>
      )}

      {CALLABLE_STATUSES.includes(order.status) && (
        <div className="mt-6">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Enregistrer un appel</h2>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              recordCallAttempt.mutate();
            }}
            className="flex flex-col gap-3 rounded-lg border border-black/10 bg-white p-4 sm:max-w-md"
          >
            <label className="flex flex-col gap-1 text-sm">
              Résultat
              <select
                value={callResult}
                onChange={(e) => setCallResult(e.target.value as CallAttemptResult)}
                className="rounded border border-black/20 px-3 py-2 text-sm"
              >
                {CALL_ATTEMPT_RESULTS.map((result) => (
                  <option key={result} value={result}>
                    {CALL_ATTEMPT_RESULT_LABELS[result]}
                  </option>
                ))}
              </select>
            </label>

            {callResult === 'CallbackScheduled' && (
              <label className="flex flex-col gap-1 text-sm">
                Prochain appel
                <input
                  type="datetime-local"
                  value={nextCallAt}
                  onChange={(e) => setNextCallAt(e.target.value)}
                  className="rounded border border-black/20 px-3 py-2 text-sm"
                  required
                />
              </label>
            )}

            <label className="flex flex-col gap-1 text-sm">
              Notes
              <textarea
                value={callNotes}
                onChange={(e) => setCallNotes(e.target.value)}
                rows={3}
                className="rounded border border-black/20 px-3 py-2 text-sm"
              />
            </label>

            <button
              type="submit"
              disabled={recordCallAttempt.isPending}
              className="self-start rounded-full bg-luna-black px-4 py-2 text-sm text-white disabled:opacity-40"
            >
              Enregistrer l'appel
            </button>

            {callError && <p className="text-sm text-red-600">{callError}</p>}
          </form>
        </div>
      )}

      {order.callAttempts.length > 0 && (
        <div className="mt-6">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Journal d'appels</h2>
          <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white text-sm">
            {order.callAttempts.map((attempt) => (
              <div key={attempt.id} className="px-4 py-2">
                <p>
                  Appel #{attempt.attemptNumber} — {CALL_ATTEMPT_RESULT_LABELS[attempt.result]}
                  <span className="ml-2 text-xs text-luna-charcoal/60">
                    {new Date(attempt.calledAtUtc).toLocaleString('fr-FR')}
                  </span>
                </p>
                {attempt.notes && <p className="text-xs text-luna-charcoal/70">Notes : {attempt.notes}</p>}
                {attempt.nextCallAtUtc && (
                  <p className="text-xs text-luna-charcoal/70">
                    Prochain appel : {new Date(attempt.nextCallAtUtc).toLocaleString('fr-FR')}
                  </p>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {order.appliedPromotions.length > 0 && (
        <div className="mt-6">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Promotions appliquées</h2>
          <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white text-sm">
            {order.appliedPromotions.map((p) => (
              <div key={p.id} className="flex items-center justify-between px-4 py-2">
                <span>{p.promotionName}</span>
                <span className="text-green-700">−{formatPrice(p.discountAmount)}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {order.statusHistory.length > 0 && (
        <div className="mt-6">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Historique</h2>
          <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white text-sm">
            {order.statusHistory.map((h) => (
              <div key={h.id} className="px-4 py-2">
                <p>
                  {ORDER_STATUS_LABELS[h.oldStatus]} → {ORDER_STATUS_LABELS[h.newStatus]}
                  <span className="ml-2 text-xs text-luna-charcoal/60">
                    {new Date(h.createdAtUtc).toLocaleString('fr-FR')}
                  </span>
                </p>
                {h.reason && <p className="text-xs text-luna-charcoal/70">Raison : {h.reason}</p>}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
