import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { shippingApi } from '../../lib/api/shipping';
import { ApiError } from '../../lib/api/client';
import type { CallAttemptResult, OrderReturnReason, OrderStatus, ShippingCarrier } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';
import { CALL_ATTEMPT_RESULT_LABELS, DELIVERY_TYPE_LABELS, ORDER_STATUS_LABELS, RETURN_REASON_LABELS } from '../../lib/format/orderLabels';
import { NORMALIZED_SHIPPING_STATUS_LABELS, SHIPPING_CARRIER_LABELS } from '../../lib/format/shippingLabels';
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
  const [shipmentCarrier, setShipmentCarrier] = useState<ShippingCarrier>('Fake');
  const [shipmentError, setShipmentError] = useState<string | null>(null);
  const [label, setLabel] = useState<string | null>(null);
  const [returnReason, setReturnReason] = useState<OrderReturnReason>('WrongSize');
  const [returnNote, setReturnNote] = useState('');

  const { data: order, isLoading, isError } = useQuery({
    queryKey: ['admin-order', id],
    queryFn: () => ordersApi.getById(id!),
    enabled: !!id,
  });

  const { data: carriers } = useQuery({
    queryKey: ['shipping-carriers'],
    queryFn: () => shippingApi.getCarriers(),
  });

  const changeStatus = useMutation({
    mutationFn: (vars: { newStatus: OrderStatus; reason: string | null; returnReason?: OrderReturnReason | null }) =>
      ordersApi.changeStatus(id!, vars),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-order', id], updated);
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      setActionError(null);
      setReturnNote('');
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

  const createShipment = useMutation({
    mutationFn: () => shippingApi.createShipment(id!, { carrier: shipmentCarrier }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-order', id] });
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      setShipmentError(null);
    },
    onError: (err) => setShipmentError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const syncShipment = useMutation({
    mutationFn: (shipmentId: string) => shippingApi.sync(shipmentId),
    onSuccess: (updatedShipment) => {
      queryClient.setQueryData(['admin-order', id], (current: typeof order) =>
        current ? { ...current, shipment: updatedShipment } : current,
      );
    },
  });

  const viewLabel = useMutation({
    mutationFn: (shipmentId: string) => shippingApi.getLabel(shipmentId),
    onSuccess: (text) => setLabel(text),
    onError: (err) => setShipmentError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
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

  function handleReturnSubmit(e: React.FormEvent) {
    e.preventDefault();
    changeStatus.mutate({ newStatus: 'Returned', reason: returnNote.trim() || null, returnReason });
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
          {order.returnReason && (
            <p className="mt-1 text-sm text-luna-charcoal/70">Cause du retour : {RETURN_REASON_LABELS[order.returnReason]}</p>
          )}
        </div>

        {order.marketingAttribution && (
          <div>
            <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Marketing</h2>
            {order.marketingAttribution.utmSource && (
              <p className="text-sm">Source : {order.marketingAttribution.utmSource}</p>
            )}
            {order.marketingAttribution.utmCampaign && (
              <p className="text-sm">Campagne : {order.marketingAttribution.utmCampaign}</p>
            )}
            {order.marketingAttribution.utmContent && (
              <p className="text-sm">Annonce : {order.marketingAttribution.utmContent}</p>
            )}
            {order.marketingAttribution.utmMedium && (
              <p className="text-sm">Support : {order.marketingAttribution.utmMedium}</p>
            )}
            {order.marketingAttribution.referrer && (
              <p className="text-sm text-luna-charcoal/70">Référent : {order.marketingAttribution.referrer}</p>
            )}
          </div>
        )}
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
            <form
              onSubmit={handleReturnSubmit}
              className="mt-3 flex flex-col gap-2 rounded-lg border border-black/10 bg-white p-4 sm:max-w-md"
            >
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

      <div className="mt-6">
        <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Expédition</h2>

        {order.shipment ? (
          <div className="flex flex-col gap-3 rounded-lg border border-black/10 bg-white p-4 text-sm sm:max-w-md">
            <p>
              <span className="font-medium">{SHIPPING_CARRIER_LABELS[order.shipment.carrier]}</span>
              {' — '}
              {NORMALIZED_SHIPPING_STATUS_LABELS[order.shipment.normalizedStatus]}
            </p>
            {order.shipment.trackingNumber && (
              <p className="text-luna-charcoal/70">Numéro de suivi : {order.shipment.trackingNumber}</p>
            )}

            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                disabled={syncShipment.isPending}
                onClick={() => syncShipment.mutate(order.shipment!.id)}
                className="rounded-full border border-luna-black px-4 py-2 text-xs disabled:opacity-40"
              >
                {syncShipment.isPending ? 'Actualisation...' : 'Actualiser le suivi'}
              </button>
              <button
                type="button"
                disabled={viewLabel.isPending}
                onClick={() => viewLabel.mutate(order.shipment!.id)}
                className="rounded-full border border-luna-black px-4 py-2 text-xs disabled:opacity-40"
              >
                Voir l'étiquette
              </button>
            </div>

            {label && <pre className="whitespace-pre-wrap rounded border border-black/10 bg-luna-cream p-3 text-xs">{label}</pre>}

            {order.shipment.trackingEvents.length > 0 && (
              <div className="flex flex-col divide-y divide-black/5 border-t border-black/10 pt-2">
                {order.shipment.trackingEvents.map((event) => (
                  <div key={event.id} className="py-1.5">
                    <p>
                      {NORMALIZED_SHIPPING_STATUS_LABELS[event.normalizedStatus]}
                      <span className="ml-2 text-xs text-luna-charcoal/60">
                        {new Date(event.occurredAtUtc).toLocaleString('fr-FR')}
                      </span>
                    </p>
                    {event.description && <p className="text-xs text-luna-charcoal/70">{event.description}</p>}
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : order.status === 'ReadyToShip' ? (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              createShipment.mutate();
            }}
            className="flex flex-col gap-3 rounded-lg border border-black/10 bg-white p-4 sm:max-w-md"
          >
            <label className="flex flex-col gap-1 text-sm">
              Transporteur
              <select
                value={shipmentCarrier}
                onChange={(e) => setShipmentCarrier(e.target.value as ShippingCarrier)}
                className="rounded border border-black/20 px-3 py-2 text-sm"
              >
                {carriers?.map((c) => (
                  <option key={c.carrier} value={c.carrier} disabled={!c.isConfigured}>
                    {SHIPPING_CARRIER_LABELS[c.carrier]}
                    {!c.isConfigured ? ' (non disponible)' : ''}
                  </option>
                ))}
              </select>
            </label>

            <button
              type="submit"
              disabled={createShipment.isPending}
              className="self-start rounded-full bg-luna-black px-4 py-2 text-sm text-white disabled:opacity-40"
            >
              {createShipment.isPending ? 'Création...' : "Créer l'expédition"}
            </button>
          </form>
        ) : (
          <p className="text-sm text-luna-charcoal/60">
            Aucune expédition. La commande doit être "Prête à expédier" pour en créer une.
          </p>
        )}

        {shipmentError && <p className="mt-2 text-sm text-red-600">{shipmentError}</p>}
      </div>

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
