import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { ordersApi } from '../../lib/api/orders';
import { shippingApi } from '../../lib/api/shipping';
import { catalogApi } from '../../lib/api/catalog';
import { ApiError } from '../../lib/api/client';
import type { OrderDetailDto, ProductDetailDto, ProductVariantDto, ShippingCarrier } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';
import { CALL_ATTEMPT_RESULT_LABELS, DELIVERY_TYPE_LABELS, ORDER_STATUS_LABELS, RETURN_REASON_LABELS } from '../../lib/format/orderLabels';
import { NORMALIZED_SHIPPING_STATUS_LABELS, SHIPPING_CARRIER_LABELS } from '../../lib/format/shippingLabels';
import { ALLOWED_TRANSITIONS } from '../../lib/orders/transitions';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';
import { OrderActionsPanel } from '../components/OrderActionsPanel';

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [shipmentCarrier, setShipmentCarrier] = useState<ShippingCarrier>('Fake');
  const [shipmentError, setShipmentError] = useState<string | null>(null);
  const [label, setLabel] = useState<string | null>(null);

  const [notesDraft, setNotesDraft] = useState<string | null>(null);
  const [notesError, setNotesError] = useState<string | null>(null);

  const [showAddItem, setShowAddItem] = useState(false);
  const [productSearch, setProductSearch] = useState('');
  const [openProductId, setOpenProductId] = useState<string | null>(null);
  const [productDetail, setProductDetail] = useState<ProductDetailDto | null>(null);
  const [selectedVariant, setSelectedVariant] = useState<ProductVariantDto | null>(null);
  const [variantQuantity, setVariantQuantity] = useState(1);
  const [itemsError, setItemsError] = useState<string | null>(null);

  const [manualDiscountDraft, setManualDiscountDraft] = useState<string | null>(null);
  const [freeShippingDraft, setFreeShippingDraft] = useState<boolean | null>(null);
  const [negotiationError, setNegotiationError] = useState<string | null>(null);

  const { data: order, isLoading, isError } = useQuery({
    queryKey: ['admin-order', id],
    queryFn: () => ordersApi.getById(id!),
    enabled: !!id,
  });

  const { data: carriers } = useQuery({
    queryKey: ['shipping-carriers'],
    queryFn: () => shippingApi.getCarriers(),
  });

  const { data: products } = useQuery({
    queryKey: ['admin-products-for-order-edit'],
    queryFn: () => catalogApi.getProducts({ pageSize: 100 }),
    enabled: showAddItem,
  });

  function handleOrderChanged(updated: OrderDetailDto) {
    queryClient.setQueryData(['admin-order', id], updated);
    queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
  }

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

  const updateNotes = useMutation({
    mutationFn: (notes: string | null) => ordersApi.updateNotes(id!, { notes }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-order', id], updated);
      setNotesDraft(null);
      setNotesError(null);
    },
    onError: (err) => setNotesError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const addItem = useMutation({
    mutationFn: () => ordersApi.addItem(id!, { productVariantId: selectedVariant!.id, quantity: variantQuantity }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-order', id], updated);
      setItemsError(null);
      setOpenProductId(null);
      setProductDetail(null);
      setSelectedVariant(null);
      setVariantQuantity(1);
      setShowAddItem(false);
    },
    onError: (err) => setItemsError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const removeItem = useMutation({
    mutationFn: (itemId: string) => ordersApi.removeItem(id!, itemId),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-order', id], updated);
      setItemsError(null);
    },
    onError: (err) => setItemsError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const updateNegotiation = useMutation({
    mutationFn: (vars: { manualDiscountAmount: number | null; freeShipping: boolean }) =>
      ordersApi.updateNegotiation(id!, vars),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-order', id], updated);
      setManualDiscountDraft(null);
      setFreeShippingDraft(null);
      setNegotiationError(null);
    },
    onError: (err) => setNegotiationError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  if (isLoading) {
    return <p className="text-sm text-luna-charcoal/60">Chargement...</p>;
  }

  if (isError || !order) {
    return <PagePlaceholder title="Commande introuvable" />;
  }

  async function openProduct(productId: string, slug: string) {
    if (openProductId === productId) {
      setOpenProductId(null);
      setProductDetail(null);
      setSelectedVariant(null);
      return;
    }
    setOpenProductId(productId);
    setSelectedVariant(null);
    setVariantQuantity(1);
    const detail = await catalogApi.getProductBySlug(slug);
    setProductDetail(detail);
  }

  return (
    <div>
      <Link to="/admin/orders" className="mb-4 inline-block text-sm underline">
        ← Retour aux commandes
      </Link>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold">{order.orderNumber}</h1>
        <div className="flex items-center gap-2">
          {order.createdByUserId && (
            <span className="rounded-full bg-luna-rose px-3 py-1 text-sm font-medium text-luna-accent-dark">
              Commande téléphonique
            </span>
          )}
          <span className="rounded-full bg-luna-cream px-3 py-1 text-sm font-medium">
            {ORDER_STATUS_LABELS[order.status]}
          </span>
        </div>
      </div>

      <div className="mt-6 grid gap-6 sm:grid-cols-2">
        <div>
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Client</h2>
          <p className="text-sm">
            {order.firstName} {order.lastName}
          </p>
          <p className="text-sm">{order.phone}</p>
          <p className="text-sm">
            {[order.address, order.commune, order.wilaya].filter(Boolean).join(', ')}
          </p>
          <p className="text-sm">{DELIVERY_TYPE_LABELS[order.deliveryType]}</p>
          <div className="mt-2">
            <label className="mb-1 block text-xs font-medium text-luna-charcoal/60">Remarque</label>
            <textarea
              value={notesDraft ?? order.notes ?? ''}
              onChange={(e) => setNotesDraft(e.target.value)}
              placeholder="Ex. : veut du bleu plutôt que noir, rappeler après 18h..."
              rows={2}
              className="w-full rounded border border-black/20 px-2 py-1.5 text-sm"
            />
            {notesDraft !== null && notesDraft !== (order.notes ?? '') && (
              <div className="mt-1 flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => updateNotes.mutate(notesDraft.trim() || null)}
                  disabled={updateNotes.isPending}
                  className="rounded-full bg-luna-black px-3 py-1 text-xs text-white disabled:opacity-40"
                >
                  {updateNotes.isPending ? 'Enregistrement...' : 'Enregistrer'}
                </button>
                <button type="button" onClick={() => setNotesDraft(null)} className="text-xs underline">
                  Annuler
                </button>
              </div>
            )}
            {notesError && <p className="mt-1 text-xs text-red-600">{notesError}</p>}
          </div>
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
              <div className="flex items-center gap-3">
                <span>{formatPrice(item.lineTotal)}</span>
                {order.isEditable && order.items.length > 1 && (
                  <button
                    type="button"
                    onClick={() => removeItem.mutate(item.id)}
                    disabled={removeItem.isPending}
                    className="text-xs text-red-600 underline disabled:opacity-40"
                  >
                    Retirer
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
        {itemsError && <p className="mt-2 text-sm text-red-600">{itemsError}</p>}

        {order.isEditable && (
          <div className="mt-3">
            {!showAddItem ? (
              <button type="button" onClick={() => setShowAddItem(true)} className="text-xs underline">
                + Ajouter un article
              </button>
            ) : (
              <div className="rounded-lg border border-black/10 bg-white p-3">
                <input
                  value={productSearch}
                  onChange={(e) => setProductSearch(e.target.value)}
                  placeholder="Rechercher un produit..."
                  className="mb-2 w-full rounded border border-black/20 px-2 py-1.5 text-sm"
                />
                <div className="max-h-56 overflow-y-auto rounded border border-black/10">
                  {(products?.items ?? [])
                    .filter((p) => p.name.toLowerCase().includes(productSearch.trim().toLowerCase()))
                    .map((p) => (
                      <div key={p.id} className="border-b border-black/5 last:border-0">
                        <button
                          type="button"
                          onClick={() => openProduct(p.id, p.slug)}
                          className="flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-luna-cream/50"
                        >
                          <span>{p.name}</span>
                          <span className="text-xs text-luna-charcoal/60">{formatPrice(p.price)}</span>
                        </button>

                        {openProductId === p.id && productDetail && (
                          <div className="bg-luna-cream/40 px-3 py-3">
                            <div className="flex flex-wrap gap-1.5">
                              {productDetail.variants
                                .filter((v) => v.isActive)
                                .map((v) => (
                                  <button
                                    key={v.id}
                                    type="button"
                                    onClick={() => {
                                      setSelectedVariant(v);
                                      setVariantQuantity(1);
                                    }}
                                    disabled={v.availableQuantity === 0}
                                    className={`rounded border px-2 py-1 text-xs disabled:cursor-not-allowed disabled:opacity-40 ${
                                      selectedVariant?.id === v.id ? 'border-luna-black bg-luna-black text-white' : 'border-black/20 bg-white'
                                    }`}
                                  >
                                    {v.color} · {v.size} ({v.availableQuantity} en stock)
                                  </button>
                                ))}
                            </div>

                            {selectedVariant && (
                              <div className="mt-3 flex items-center gap-3">
                                <div className="flex items-center rounded border border-black/20">
                                  <button type="button" className="p-2" onClick={() => setVariantQuantity((q) => Math.max(1, q - 1))}>
                                    −
                                  </button>
                                  <span className="w-8 text-center text-sm">{variantQuantity}</span>
                                  <button
                                    type="button"
                                    className="p-2"
                                    onClick={() => setVariantQuantity((q) => Math.min(selectedVariant.availableQuantity, q + 1))}
                                  >
                                    +
                                  </button>
                                </div>
                                <button
                                  type="button"
                                  onClick={() => addItem.mutate()}
                                  disabled={addItem.isPending}
                                  className="rounded-full bg-luna-black px-4 py-1.5 text-xs text-white disabled:opacity-40"
                                >
                                  {addItem.isPending ? 'Ajout...' : 'Ajouter à la commande'}
                                </button>
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    ))}
                </div>
                <button
                  type="button"
                  onClick={() => {
                    setShowAddItem(false);
                    setOpenProductId(null);
                    setProductDetail(null);
                    setSelectedVariant(null);
                  }}
                  className="mt-2 text-xs underline"
                >
                  Fermer
                </button>
              </div>
            )}
          </div>
        )}
      </div>

      {order.isEditable && (
        <div className="mt-6">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Négociation téléphonique</h2>
          <div className="flex flex-col gap-2 rounded-lg border border-luna-accent/30 bg-luna-rose/30 p-4 sm:max-w-md">
            <label className="text-sm">
              Remise négociée (DA)
              <input
                type="number"
                min={0}
                value={manualDiscountDraft ?? (order.manualDiscountAmount?.toString() ?? '')}
                onChange={(e) => setManualDiscountDraft(e.target.value)}
                placeholder="ex. 500"
                className="mt-1 w-full rounded border border-black/20 bg-white px-2 py-1.5 text-sm"
              />
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={freeShippingDraft ?? order.negotiatedFreeShipping}
                onChange={(e) => setFreeShippingDraft(e.target.checked)}
              />
              Livraison offerte (négociée)
            </label>
            {negotiationError && <p className="text-xs text-red-600">{negotiationError}</p>}
            <button
              type="button"
              onClick={() =>
                updateNegotiation.mutate({
                  manualDiscountAmount: Number(manualDiscountDraft ?? order.manualDiscountAmount ?? 0) || null,
                  freeShipping: freeShippingDraft ?? order.negotiatedFreeShipping,
                })
              }
              disabled={updateNegotiation.isPending}
              className="mt-1 w-fit rounded-full bg-luna-black px-4 py-2 text-sm text-white disabled:opacity-40"
            >
              {updateNegotiation.isPending ? 'Enregistrement...' : 'Enregistrer'}
            </button>
          </div>
        </div>
      )}

      {ALLOWED_TRANSITIONS[order.status].length > 0 && (
        <div className="mt-6 sm:max-w-md">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Actions</h2>
          <OrderActionsPanel order={order} onChanged={handleOrderChanged} />
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
                    {h.changedByUserName ? ` · ${h.changedByUserName}` : ' · Automatique'}
                  </span>
                </p>
                {h.reason && <p className="text-xs text-luna-charcoal/70">Raison : {h.reason}</p>}
              </div>
            ))}
          </div>
        </div>
      )}

      {order.callAttempts.length > 0 && (
        <div className="mt-6">
          <h2 className="mb-2 text-sm font-semibold uppercase text-luna-charcoal/60">Journal d'appels</h2>
          <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white text-sm">
            {order.callAttempts.map((a) => (
              <div key={a.id} className="px-4 py-2">
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
        </div>
      )}
    </div>
  );
}
