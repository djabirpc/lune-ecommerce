import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Minus, Plus, Trash2 } from 'lucide-react';

import { catalogApi } from '../../lib/api/catalog';
import { ordersApi } from '../../lib/api/orders';
import { shippingRatesApi } from '../../lib/api/shipping';
import { ApiError } from '../../lib/api/client';
import type { CreateOrderRequest, DeliveryType, ProductDetailDto, ProductVariantDto } from '../../lib/api/types';
import { formatPrice } from '../../lib/format/price';
import { DELIVERY_TYPE_LABELS } from '../../lib/format/orderLabels';
import { ALGERIAN_WILAYAS } from '../../lib/data/wilayas';

interface LineItem {
  variantId: string;
  productName: string;
  color: string;
  size: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  availableQuantity: number;
}

function addOrMergeLine(lines: LineItem[], next: LineItem): LineItem[] {
  const existing = lines.find((l) => l.variantId === next.variantId);
  if (!existing) return [...lines, next];
  return lines.map((l) =>
    l.variantId === next.variantId
      ? { ...l, quantity: Math.min(l.quantity + next.quantity, l.availableQuantity) }
      : l,
  );
}

export function CreateOrderPage() {
  const navigate = useNavigate();

  const [productSearch, setProductSearch] = useState('');
  const [openProductId, setOpenProductId] = useState<string | null>(null);
  const [productDetail, setProductDetail] = useState<ProductDetailDto | null>(null);
  const [selectedVariant, setSelectedVariant] = useState<ProductVariantDto | null>(null);
  const [variantQuantity, setVariantQuantity] = useState(1);
  const [lines, setLines] = useState<LineItem[]>([]);

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [wilaya, setWilaya] = useState('');
  const [commune, setCommune] = useState('');
  const [address, setAddress] = useState('');
  const [deliveryType, setDeliveryType] = useState<DeliveryType>('HomeDelivery');
  const [notes, setNotes] = useState('');
  const [couponCode, setCouponCode] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const { data: products } = useQuery({
    queryKey: ['admin-products-for-order'],
    queryFn: () => catalogApi.getProducts({ pageSize: 100 }),
  });

  const { data: shippingQuote } = useQuery({
    queryKey: ['admin-order-shipping-quote', wilaya, deliveryType],
    queryFn: () => shippingRatesApi.getQuote(wilaya, deliveryType),
    enabled: Boolean(wilaya),
  });

  const filteredProducts = useMemo(() => {
    const term = productSearch.trim().toLowerCase();
    const items = products?.items ?? [];
    return term ? items.filter((p) => p.name.toLowerCase().includes(term)) : items;
  }, [products, productSearch]);

  const subtotal = lines.reduce((sum, l) => sum + l.unitPrice * l.quantity, 0);

  const submit = useMutation({
    mutationFn: (request: CreateOrderRequest) => ordersApi.createAdmin(request),
    onSuccess: (order) => navigate(`/admin/orders/${order.id}`),
    onError: (err) => setFormError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

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

  function addLine() {
    if (!selectedVariant || !productDetail) return;
    setLines((prev) =>
      addOrMergeLine(prev, {
        variantId: selectedVariant.id,
        productName: productDetail.name,
        color: selectedVariant.color,
        size: selectedVariant.size,
        sku: selectedVariant.sku,
        unitPrice: selectedVariant.price,
        quantity: Math.min(variantQuantity, selectedVariant.availableQuantity),
        availableQuantity: selectedVariant.availableQuantity,
      }),
    );
    setSelectedVariant(null);
    setVariantQuantity(1);
  }

  function removeLine(variantId: string) {
    setLines((prev) => prev.filter((l) => l.variantId !== variantId));
  }

  function updateLineQuantity(variantId: string, quantity: number) {
    setLines((prev) =>
      prev.map((l) => (l.variantId === variantId ? { ...l, quantity: Math.max(1, Math.min(quantity, l.availableQuantity)) } : l)),
    );
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);

    if (lines.length === 0) {
      setFormError('Ajoutez au moins un article à la commande.');
      return;
    }

    submit.mutate({
      firstName,
      lastName,
      phone,
      wilaya,
      commune,
      address: address || null,
      deliveryType,
      notes: notes || null,
      items: lines.map((l) => ({ productVariantId: l.variantId, quantity: l.quantity })),
      couponCode: couponCode.trim() || null,
    });
  }

  return (
    <div>
      <h1 className="mb-1 text-xl font-semibold">Nouvelle commande</h1>
      <p className="mb-4 text-sm text-luna-charcoal/60">
        Pour une commande prise par téléphone — elle sera créée directement au statut "Confirmée".
      </p>

      <form onSubmit={handleSubmit} className="grid gap-6 lg:grid-cols-[1fr_22rem]">
        <div className="flex flex-col gap-6">
          <div className="rounded-lg border border-black/10 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Articles</h2>

            <input
              value={productSearch}
              onChange={(e) => setProductSearch(e.target.value)}
              placeholder="Rechercher un produit..."
              className="mb-3 w-full rounded border border-black/20 px-3 py-2 text-sm"
            />

            <div className="max-h-72 overflow-y-auto rounded border border-black/10">
              {filteredProducts.map((p) => (
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
                            <button
                              type="button"
                              className="p-2"
                              onClick={() => setVariantQuantity((q) => Math.max(1, q - 1))}
                            >
                              <Minus className="h-3 w-3" />
                            </button>
                            <span className="w-8 text-center text-sm">{variantQuantity}</span>
                            <button
                              type="button"
                              className="p-2"
                              onClick={() => setVariantQuantity((q) => Math.min(selectedVariant.availableQuantity, q + 1))}
                            >
                              <Plus className="h-3 w-3" />
                            </button>
                          </div>
                          <button
                            type="button"
                            onClick={addLine}
                            className="rounded-full bg-luna-black px-4 py-1.5 text-xs text-white"
                          >
                            Ajouter à la commande
                          </button>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              ))}
              {filteredProducts.length === 0 && (
                <p className="px-3 py-6 text-center text-sm text-luna-charcoal/60">Aucun produit trouvé.</p>
              )}
            </div>
          </div>

          <div className="rounded-lg border border-black/10 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Client</h2>
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-xs font-medium">Prénom</label>
                <input value={firstName} onChange={(e) => setFirstName(e.target.value)} required className="w-full rounded border border-black/20 px-2 py-1.5 text-sm" />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Nom</label>
                <input value={lastName} onChange={(e) => setLastName(e.target.value)} required className="w-full rounded border border-black/20 px-2 py-1.5 text-sm" />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Téléphone</label>
                <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="0551234567" required className="w-full rounded border border-black/20 px-2 py-1.5 text-sm" />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Wilaya</label>
                <select value={wilaya} onChange={(e) => setWilaya(e.target.value)} required className="w-full rounded border border-black/20 px-2 py-1.5 text-sm">
                  <option value="" disabled>
                    Sélectionnez une wilaya
                  </option>
                  {ALGERIAN_WILAYAS.map((w) => (
                    <option key={w} value={w}>
                      {w}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Commune</label>
                <input value={commune} onChange={(e) => setCommune(e.target.value)} required className="w-full rounded border border-black/20 px-2 py-1.5 text-sm" />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Adresse (optionnel)</label>
                <input value={address} onChange={(e) => setAddress(e.target.value)} className="w-full rounded border border-black/20 px-2 py-1.5 text-sm" />
              </div>
            </div>

            <div className="mt-3">
              <label className="mb-1 block text-xs font-medium">Type de livraison</label>
              <div className="grid grid-cols-2 gap-2">
                {(Object.keys(DELIVERY_TYPE_LABELS) as DeliveryType[]).map((type) => (
                  <label
                    key={type}
                    className="flex h-10 cursor-pointer items-center justify-center gap-2 rounded border border-black/20 text-sm has-[:checked]:border-luna-black has-[:checked]:bg-luna-black has-[:checked]:text-white"
                  >
                    <input
                      type="radio"
                      value={type}
                      checked={deliveryType === type}
                      onChange={() => setDeliveryType(type)}
                      className="sr-only"
                    />
                    {DELIVERY_TYPE_LABELS[type]}
                  </label>
                ))}
              </div>
            </div>

            <div className="mt-3">
              <label className="mb-1 block text-xs font-medium">Note (optionnel)</label>
              <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} className="w-full rounded border border-black/20 px-2 py-1.5 text-sm" />
            </div>
          </div>
        </div>

        <aside className="h-fit rounded-lg border border-black/10 bg-white p-4 lg:sticky lg:top-6">
          <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Résumé</h2>

          {lines.length === 0 && <p className="text-sm text-luna-charcoal/60">Aucun article ajouté.</p>}

          <ul className="flex flex-col gap-2">
            {lines.map((l) => (
              <li key={l.variantId} className="flex items-start justify-between gap-2 text-xs">
                <div className="min-w-0">
                  <p className="truncate font-medium text-luna-black">{l.productName}</p>
                  <p className="text-luna-charcoal/60">
                    {l.color} · {l.size}
                  </p>
                  <div className="mt-1 flex items-center gap-1.5">
                    <button
                      type="button"
                      onClick={() => updateLineQuantity(l.variantId, l.quantity - 1)}
                      className="rounded border border-black/20 p-0.5"
                    >
                      <Minus className="h-2.5 w-2.5" />
                    </button>
                    <span className="w-5 text-center">{l.quantity}</span>
                    <button
                      type="button"
                      onClick={() => updateLineQuantity(l.variantId, l.quantity + 1)}
                      className="rounded border border-black/20 p-0.5"
                    >
                      <Plus className="h-2.5 w-2.5" />
                    </button>
                  </div>
                </div>
                <div className="flex shrink-0 flex-col items-end gap-1">
                  <span className="font-medium">{formatPrice(l.unitPrice * l.quantity)}</span>
                  <button type="button" onClick={() => removeLine(l.variantId)} aria-label="Retirer">
                    <Trash2 className="h-3.5 w-3.5 text-luna-charcoal/40 hover:text-red-600" />
                  </button>
                </div>
              </li>
            ))}
          </ul>

          <dl className="mt-4 space-y-1.5 border-t border-black/10 pt-3 text-sm">
            <div className="flex justify-between">
              <dt className="text-luna-charcoal/60">Sous-total</dt>
              <dd>{formatPrice(subtotal)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-luna-charcoal/60">Livraison</dt>
              <dd>{shippingQuote ? formatPrice(shippingQuote.price) : 'Selon la wilaya'}</dd>
            </div>
          </dl>

          <div className="mt-3">
            <label className="mb-1 block text-xs font-medium">Code promo (optionnel)</label>
            <input
              value={couponCode}
              onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
              className="w-full rounded border border-black/20 px-2 py-1.5 text-sm"
            />
          </div>

          <p className="mt-3 text-[11px] text-luna-charcoal/50">
            Les remises, offres et le total final sont calculés automatiquement par le serveur à la création.
          </p>

          {formError && <p className="mt-3 text-sm text-red-600">{formError}</p>}

          <button
            type="submit"
            disabled={submit.isPending}
            className="mt-4 w-full rounded-full bg-luna-black px-5 py-2.5 text-sm text-white disabled:opacity-40"
          >
            {submit.isPending ? 'Création...' : 'Créer la commande'}
          </button>
        </aside>
      </form>
    </div>
  );
}
