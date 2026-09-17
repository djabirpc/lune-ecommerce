import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { ApiError } from '../../lib/api/client';
import type { OrderDetailDto, OrderReturnReason, OrderStatus } from '../../lib/api/types';
import { RETURN_REASON_LABELS } from '../../lib/format/orderLabels';
import { ALLOWED_TRANSITIONS, ORDER_ACTION_LABELS, requiresReason } from '../../lib/orders/transitions';
import { ReasonModal } from './ReasonModal';

// Status-transition buttons + reason modal + the "mark returned" mini-form, shared by the full
// OrderDetailPage and the compact Order/Confirmation Summary drawers — a single implementation of
// "what actions does this order's current status allow" instead of three forked copies.
export function OrderActionsPanel({ order, onChanged }: { order: OrderDetailDto; onChanged: (updated: OrderDetailDto) => void }) {
  const [actionError, setActionError] = useState<string | null>(null);
  const [pendingTransition, setPendingTransition] = useState<OrderStatus | null>(null);
  const [returnReason, setReturnReason] = useState<OrderReturnReason>('WrongSize');
  const [returnNote, setReturnNote] = useState('');

  const changeStatus = useMutation({
    mutationFn: (vars: { newStatus: OrderStatus; reason: string | null; returnReason?: OrderReturnReason | null }) =>
      ordersApi.changeStatus(order.id, vars),
    onSuccess: (updated) => {
      onChanged(updated);
      setActionError(null);
      setReturnNote('');
    },
    onError: (err) => setActionError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const nextStatuses = ALLOWED_TRANSITIONS[order.status];

  function handleTransition(newStatus: OrderStatus) {
    if (requiresReason(newStatus)) {
      setPendingTransition(newStatus);
      return;
    }
    changeStatus.mutate({ newStatus, reason: null });
  }

  function confirmPendingTransition(reason: string) {
    if (!pendingTransition) return;
    changeStatus.mutate({ newStatus: pendingTransition, reason: reason || null });
    setPendingTransition(null);
  }

  function handleReturnSubmit(e: React.FormEvent) {
    e.preventDefault();
    changeStatus.mutate({ newStatus: 'Returned', reason: returnNote.trim() || null, returnReason });
  }

  if (nextStatuses.length === 0) {
    return null;
  }

  return (
    <div>
      <div className="flex flex-wrap gap-2">
        {nextStatuses
          .filter((next) => next !== 'Returned')
          .map((next) => (
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

      {nextStatuses.includes('Returned') && (
        <form onSubmit={handleReturnSubmit} className="mt-3 flex flex-col gap-2 rounded-lg border border-black/10 bg-white p-4">
          <h3 className="text-sm font-medium">Marquer retournée</h3>
          <label className="flex flex-col gap-1 text-sm">
            Cause du retour
            <select
              value={returnReason}
              onChange={(e) => setReturnReason(e.target.value as OrderReturnReason)}
              className="rounded border border-black/20 px-2 py-1 text-sm"
            >
              {(Object.keys(RETURN_REASON_LABELS) as OrderReturnReason[]).map((r) => (
                <option key={r} value={r}>
                  {RETURN_REASON_LABELS[r]}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-sm">
            Note (optionnel)
            <input
              type="text"
              value={returnNote}
              onChange={(e) => setReturnNote(e.target.value)}
              className="rounded border border-black/20 px-2 py-1 text-sm"
            />
          </label>
          <button
            type="submit"
            disabled={changeStatus.isPending}
            className="mt-1 w-fit rounded-full border border-luna-black px-4 py-2 text-sm disabled:opacity-40"
          >
            Confirmer le retour
          </button>
        </form>
      )}

      {pendingTransition && (
        <ReasonModal
          title={`Raison pour "${ORDER_ACTION_LABELS[pendingTransition]}"`}
          onConfirm={confirmPendingTransition}
          onCancel={() => setPendingTransition(null)}
        />
      )}
    </div>
  );
}
