import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { promotionsApi } from '../../lib/api/promotions';
import { catalogApi } from '../../lib/api/catalog';
import { ApiError } from '../../lib/api/client';
import type { PromotionDetailDto, PromotionType, SavePromotionRequest } from '../../lib/api/types';
import { PROMOTION_TYPE_LABELS } from '../../lib/format/promotionLabels';
import { formatPrice } from '../../lib/format/price';

const PROMOTION_TYPES: PromotionType[] = [
  'ProductDiscount',
  'CategoryDiscount',
  'FlashSale',
  'PercentageDiscount',
  'FixedAmountDiscount',
  'BuyXGetY',
  'FreeShipping',
  'Coupon',
];

const PERCENTAGE_TYPES: PromotionType[] = ['ProductDiscount', 'CategoryDiscount', 'FlashSale', 'PercentageDiscount'];

function toDateTimeLocal(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

interface FormState {
  name: string;
  description: string;
  type: PromotionType;
  percentageValue: string;
  fixedAmountValue: string;
  buyQuantity: string;
  getQuantity: string;
  couponCode: string;
  startsAtUtc: string;
  endsAtUtc: string;
  isActive: boolean;
  priority: string;
  productIds: string[];
  categoryIds: string[];
}

function emptyForm(): FormState {
  const now = new Date();
  const inAWeek = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);
  return {
    name: '',
    description: '',
    type: 'PercentageDiscount',
    percentageValue: '',
    fixedAmountValue: '',
    buyQuantity: '',
    getQuantity: '',
    couponCode: '',
    startsAtUtc: toDateTimeLocal(now.toISOString()),
    endsAtUtc: toDateTimeLocal(inAWeek.toISOString()),
    isActive: true,
    priority: '0',
    productIds: [],
    categoryIds: [],
  };
}

function toFormState(p: PromotionDetailDto): FormState {
  return {
    name: p.name,
    description: p.description ?? '',
    type: p.type,
    percentageValue: p.percentageValue?.toString() ?? '',
    fixedAmountValue: p.fixedAmountValue?.toString() ?? '',
    buyQuantity: p.buyQuantity?.toString() ?? '',
    getQuantity: p.getQuantity?.toString() ?? '',
    couponCode: p.couponCode ?? '',
    startsAtUtc: toDateTimeLocal(p.startsAtUtc),
    endsAtUtc: toDateTimeLocal(p.endsAtUtc),
    isActive: p.isActive,
    priority: p.priority.toString(),
    productIds: p.productIds,
    categoryIds: p.categoryIds,
  };
}

function toRequest(form: FormState): SavePromotionRequest {
  return {
    name: form.name,
    description: form.description || null,
    type: form.type,
    percentageValue: form.percentageValue ? Number(form.percentageValue) : null,
    fixedAmountValue: form.fixedAmountValue ? Number(form.fixedAmountValue) : null,
    buyQuantity: form.buyQuantity ? Number(form.buyQuantity) : null,
    getQuantity: form.getQuantity ? Number(form.getQuantity) : null,
    couponCode: form.couponCode || null,
    startsAtUtc: new Date(form.startsAtUtc).toISOString(),
    endsAtUtc: new Date(form.endsAtUtc).toISOString(),
    isActive: form.isActive,
    priority: form.priority ? Number(form.priority) : 0,
    productIds: form.productIds,
    categoryIds: form.categoryIds,
  };
}

export function PromotionsPage() {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>(emptyForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const { data: promotions, isLoading } = useQuery({
    queryKey: ['admin-promotions'],
    queryFn: () => promotionsApi.getPaged({ includeInactive: true, pageSize: 100 }),
  });

  const { data: categories } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: () => catalogApi.getCategories({ includeInactive: true }),
  });

  const { data: products } = useQuery({
    queryKey: ['admin-products-for-promotions'],
    queryFn: () => catalogApi.getProducts({ includeInactive: true, pageSize: 100 }),
  });

  const save = useMutation({
    mutationFn: (request: SavePromotionRequest) =>
      editingId ? promotionsApi.update(editingId, request) : promotionsApi.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-promotions'] });
      setForm(emptyForm());
      setEditingId(null);
      setFormError(null);
    },
    onError: (err) => setFormError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const toggleActive = useMutation({
    mutationFn: async (id: string) => {
      const detail = await promotionsApi.getById(id);
      return promotionsApi.update(id, { ...toRequest(toFormState(detail)), isActive: !detail.isActive });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-promotions'] }),
  });

  async function startEdit(id: string) {
    const detail = await promotionsApi.getById(id);
    setForm(toFormState(detail));
    setEditingId(id);
    setFormError(null);
  }

  function cancelEdit() {
    setForm(emptyForm());
    setEditingId(null);
    setFormError(null);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    save.mutate(toRequest(form));
  }

  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold">Promotions</h1>

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-lg border border-black/10 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">
            {editingId ? 'Modifier la promotion' : 'Nouvelle promotion'}
          </h2>

          <form onSubmit={handleSubmit} className="flex flex-col gap-3">
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-xs font-medium">Nom</label>
                <input
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                  required
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Type</label>
                <select
                  value={form.type}
                  onChange={(e) => setForm({ ...form, type: e.target.value as PromotionType })}
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                >
                  {PROMOTION_TYPES.map((t) => (
                    <option key={t} value={t}>
                      {PROMOTION_TYPE_LABELS[t]}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label className="mb-1 block text-xs font-medium">Description</label>
              <textarea
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                rows={2}
                className="w-full rounded border border-black/20 px-2 py-1 text-sm"
              />
            </div>

            {PERCENTAGE_TYPES.includes(form.type) && (
              <div>
                <label className="mb-1 block text-xs font-medium">Pourcentage (%)</label>
                <input
                  type="number"
                  step="0.01"
                  value={form.percentageValue}
                  onChange={(e) => setForm({ ...form, percentageValue: e.target.value })}
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
            )}

            {form.type === 'FixedAmountDiscount' && (
              <div>
                <label className="mb-1 block text-xs font-medium">Montant fixe (DA)</label>
                <input
                  type="number"
                  step="0.01"
                  value={form.fixedAmountValue}
                  onChange={(e) => setForm({ ...form, fixedAmountValue: e.target.value })}
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
            )}

            {form.type === 'BuyXGetY' && (
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="mb-1 block text-xs font-medium">Quantité achetée</label>
                  <input
                    type="number"
                    value={form.buyQuantity}
                    onChange={(e) => setForm({ ...form, buyQuantity: e.target.value })}
                    className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs font-medium">Quantité offerte</label>
                  <input
                    type="number"
                    value={form.getQuantity}
                    onChange={(e) => setForm({ ...form, getQuantity: e.target.value })}
                    className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
              </div>
            )}

            {form.type === 'Coupon' && (
              <>
                <div>
                  <label className="mb-1 block text-xs font-medium">Code promo</label>
                  <input
                    value={form.couponCode}
                    onChange={(e) => setForm({ ...form, couponCode: e.target.value.toUpperCase() })}
                    className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                  />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="mb-1 block text-xs font-medium">Pourcentage (%)</label>
                    <input
                      type="number"
                      step="0.01"
                      value={form.percentageValue}
                      onChange={(e) => setForm({ ...form, percentageValue: e.target.value, fixedAmountValue: '' })}
                      className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                    />
                  </div>
                  <div>
                    <label className="mb-1 block text-xs font-medium">Ou montant fixe (DA)</label>
                    <input
                      type="number"
                      step="0.01"
                      value={form.fixedAmountValue}
                      onChange={(e) => setForm({ ...form, fixedAmountValue: e.target.value, percentageValue: '' })}
                      className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                    />
                  </div>
                </div>
              </>
            )}

            {form.type === 'ProductDiscount' && (
              <div>
                <label className="mb-1 block text-xs font-medium">Produits concernés</label>
                <div className="max-h-40 overflow-y-auto rounded border border-black/20 p-2">
                  {products?.items.map((p) => (
                    <label key={p.id} className="flex items-center gap-2 py-0.5 text-xs">
                      <input
                        type="checkbox"
                        checked={form.productIds.includes(p.id)}
                        onChange={(e) =>
                          setForm({
                            ...form,
                            productIds: e.target.checked
                              ? [...form.productIds, p.id]
                              : form.productIds.filter((id) => id !== p.id),
                          })
                        }
                      />
                      {p.name}
                    </label>
                  ))}
                </div>
              </div>
            )}

            {form.type === 'CategoryDiscount' && (
              <div>
                <label className="mb-1 block text-xs font-medium">Catégories concernées</label>
                <div className="max-h-40 overflow-y-auto rounded border border-black/20 p-2">
                  {categories?.map((c) => (
                    <label key={c.id} className="flex items-center gap-2 py-0.5 text-xs">
                      <input
                        type="checkbox"
                        checked={form.categoryIds.includes(c.id)}
                        onChange={(e) =>
                          setForm({
                            ...form,
                            categoryIds: e.target.checked
                              ? [...form.categoryIds, c.id]
                              : form.categoryIds.filter((id) => id !== c.id),
                          })
                        }
                      />
                      {c.name}
                    </label>
                  ))}
                </div>
              </div>
            )}

            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-xs font-medium">Début</label>
                <input
                  type="datetime-local"
                  value={form.startsAtUtc}
                  onChange={(e) => setForm({ ...form, startsAtUtc: e.target.value })}
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium">Fin</label>
                <input
                  type="datetime-local"
                  value={form.endsAtUtc}
                  onChange={(e) => setForm({ ...form, endsAtUtc: e.target.value })}
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 items-end gap-3">
              <div>
                <label className="mb-1 block text-xs font-medium">Priorité</label>
                <input
                  type="number"
                  value={form.priority}
                  onChange={(e) => setForm({ ...form, priority: e.target.value })}
                  className="w-full rounded border border-black/20 px-2 py-1 text-sm"
                />
              </div>
              <label className="flex items-center gap-2 pb-1 text-xs font-medium">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                />
                Active
              </label>
            </div>

            <div className="flex gap-2">
              <button
                type="submit"
                disabled={save.isPending}
                className="mt-2 w-fit rounded-full bg-luna-black px-5 py-2 text-sm text-white disabled:opacity-40"
              >
                {save.isPending ? 'Enregistrement...' : editingId ? 'Enregistrer les modifications' : 'Créer la promotion'}
              </button>
              {editingId && (
                <button type="button" onClick={cancelEdit} className="mt-2 w-fit rounded-full border border-black/20 px-5 py-2 text-sm">
                  Annuler
                </button>
              )}
            </div>

            {formError && <p className="text-sm text-red-600">{formError}</p>}
          </form>
        </div>

        <div>
          <h2 className="mb-3 text-sm font-semibold uppercase text-luna-charcoal/60">Promotions existantes</h2>
          {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}
          <div className="flex flex-col divide-y divide-black/5 rounded-lg border border-black/10 bg-white text-sm">
            {promotions?.items.map((p) => (
              <div key={p.id} className="flex items-center justify-between gap-2 px-4 py-2">
                <div>
                  <p className="font-medium">
                    {p.name}{' '}
                    <span className="rounded-full bg-luna-cream px-2 py-0.5 text-xs">{PROMOTION_TYPE_LABELS[p.type]}</span>
                    {!p.isActive && <span className="ml-1 rounded-full bg-red-100 px-2 py-0.5 text-xs text-red-700">Inactive</span>}
                  </p>
                  <p className="text-xs text-luna-charcoal/60">
                    {p.percentageValue ? `${p.percentageValue}%` : p.fixedAmountValue ? formatPrice(p.fixedAmountValue) : ''}
                    {' · '}
                    {new Date(p.startsAtUtc).toLocaleDateString('fr-FR')} → {new Date(p.endsAtUtc).toLocaleDateString('fr-FR')}
                    {' · priorité '}
                    {p.priority}
                  </p>
                </div>
                <div className="flex shrink-0 gap-2 text-xs">
                  <button type="button" onClick={() => startEdit(p.id)} className="underline">
                    Modifier
                  </button>
                  <button type="button" onClick={() => toggleActive.mutate(p.id)} className="underline">
                    {p.isActive ? 'Désactiver' : 'Activer'}
                  </button>
                </div>
              </div>
            ))}
            {promotions?.items.length === 0 && (
              <p className="px-4 py-8 text-center text-luna-charcoal/60">Aucune promotion.</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
