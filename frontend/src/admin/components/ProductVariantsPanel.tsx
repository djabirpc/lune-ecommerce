import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';
import { inventoryApi } from '../../lib/api/inventory';
import { suppliersApi } from '../../lib/api/suppliers';
import { ApiError } from '../../lib/api/client';
import { formatPrice } from '../../lib/format/price';
import type { ProductVariantDto, SupplierDto } from '../../lib/api/types';

function marginLabel(price: number, costPrice: number | null): string {
  if (costPrice === null || costPrice <= 0) return '—';
  const margin = ((price - costPrice) / price) * 100;
  return `${margin.toFixed(0)} %`;
}

function VariantStockRow({
  variant,
  suppliers,
  onChanged,
}: {
  variant: ProductVariantDto;
  suppliers: SupplierDto[];
  onChanged: () => void;
}) {
  const [restockQty, setRestockQty] = useState(1);
  const [restockSupplierId, setRestockSupplierId] = useState('');
  const [restockUnitCost, setRestockUnitCost] = useState('');
  const [adjustDelta, setAdjustDelta] = useState(0);
  const [adjustReason, setAdjustReason] = useState('');
  const [error, setError] = useState<string | null>(null);

  const restock = useMutation({
    mutationFn: () =>
      inventoryApi.restock({
        productVariantId: variant.id,
        quantity: restockQty,
        reason: 'Réassort admin',
        supplierId: restockSupplierId || null,
        unitCost: restockUnitCost ? Number(restockUnitCost) : null,
      }),
    onSuccess: () => {
      setError(null);
      setRestockUnitCost('');
      onChanged();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  const adjust = useMutation({
    mutationFn: () =>
      inventoryApi.adjust({ productVariantId: variant.id, quantityDelta: adjustDelta, reason: adjustReason }),
    onSuccess: () => {
      setError(null);
      setAdjustDelta(0);
      setAdjustReason('');
      onChanged();
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  return (
    <tr className="border-b border-black/5 last:border-0">
      <td className="px-3 py-2">
        {variant.color} / {variant.size}
      </td>
      <td className="px-3 py-2 font-mono text-xs">{variant.sku}</td>
      <td className="px-3 py-2">{variant.costPrice !== null ? formatPrice(variant.costPrice) : '—'}</td>
      <td className="px-3 py-2">{formatPrice(variant.price)}</td>
      <td className="px-3 py-2">{marginLabel(variant.price, variant.costPrice)}</td>
      <td className="px-3 py-2 font-medium">{variant.availableQuantity}</td>
      <td className="px-3 py-2">
        <div className="flex flex-wrap items-center gap-1">
          <input
            type="number"
            min={1}
            value={restockQty}
            onChange={(e) => setRestockQty(Number(e.target.value))}
            className="w-14 rounded border border-black/20 px-1 py-0.5 text-xs"
            title="Quantité"
          />
          <select
            value={restockSupplierId}
            onChange={(e) => setRestockSupplierId(e.target.value)}
            className="w-24 rounded border border-black/20 px-1 py-0.5 text-xs"
          >
            <option value="">Fournisseur...</option>
            {suppliers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
          <input
            type="number"
            min={0}
            step="0.01"
            value={restockUnitCost}
            onChange={(e) => setRestockUnitCost(e.target.value)}
            placeholder="Prix achat"
            className="w-20 rounded border border-black/20 px-1 py-0.5 text-xs"
          />
          <button
            type="button"
            disabled={restock.isPending}
            onClick={() => restock.mutate()}
            className="rounded border border-black/20 px-2 py-0.5 text-xs disabled:opacity-40"
          >
            Réassort
          </button>
        </div>
      </td>
      <td className="px-3 py-2">
        <div className="flex flex-wrap items-center gap-1">
          <input
            type="number"
            value={adjustDelta}
            onChange={(e) => setAdjustDelta(Number(e.target.value))}
            className="w-16 rounded border border-black/20 px-1 py-0.5 text-xs"
            placeholder="±qté"
          />
          <input
            type="text"
            value={adjustReason}
            onChange={(e) => setAdjustReason(e.target.value)}
            className="w-28 rounded border border-black/20 px-1 py-0.5 text-xs"
            placeholder="Raison"
          />
          <button
            type="button"
            disabled={adjust.isPending || adjustDelta === 0 || !adjustReason}
            onClick={() => adjust.mutate()}
            className="rounded border border-black/20 px-2 py-0.5 text-xs disabled:opacity-40"
          >
            Ajuster
          </button>
        </div>
        {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
      </td>
    </tr>
  );
}

export function ProductVariantsPanel({ slug }: { slug: string }) {
  const queryClient = useQueryClient();
  const {
    data: product,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['admin-product-detail', slug],
    queryFn: () => catalogApi.getProductBySlug(slug),
  });

  const { data: suppliersResult } = useQuery({
    queryKey: ['admin-suppliers', { includeInactive: false }],
    queryFn: () => suppliersApi.getPaged({ pageSize: 100 }),
  });
  const suppliers = suppliersResult?.items ?? [];

  const [newVariant, setNewVariant] = useState({ color: '', size: '', sku: '', costPrice: '', initialQuantity: 0 });
  const [addVariantError, setAddVariantError] = useState<string | null>(null);

  const addVariant = useMutation({
    mutationFn: () =>
      catalogApi.addVariant(product!.id, {
        color: newVariant.color,
        size: newVariant.size,
        sku: newVariant.sku,
        priceOverride: null,
        costPrice: newVariant.costPrice ? Number(newVariant.costPrice) : null,
        initialQuantity: newVariant.initialQuantity,
      }),
    onSuccess: () => {
      setAddVariantError(null);
      setNewVariant({ color: '', size: '', sku: '', costPrice: '', initialQuantity: 0 });
      queryClient.invalidateQueries({ queryKey: ['admin-product-detail', slug] });
    },
    onError: (err) => setAddVariantError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  if (isLoading) return <p className="p-3 text-xs text-luna-charcoal/60">Chargement des variantes...</p>;
  if (isError || !product) return <p className="p-3 text-xs text-red-600">Impossible de charger les variantes.</p>;

  return (
    <div className="border-t border-black/10 bg-luna-cream/30 p-3">
      <table className="w-full text-left text-xs">
        <thead className="text-luna-charcoal/60">
          <tr>
            <th className="px-3 py-1">Variante</th>
            <th className="px-3 py-1">SKU</th>
            <th className="px-3 py-1">Prix d&apos;achat</th>
            <th className="px-3 py-1">Prix de vente</th>
            <th className="px-3 py-1">Marge</th>
            <th className="px-3 py-1">Stock</th>
            <th className="px-3 py-1">Réassort</th>
            <th className="px-3 py-1">Ajustement</th>
          </tr>
        </thead>
        <tbody>
          {product.variants.map((v) => (
            <VariantStockRow
              key={v.id}
              variant={v}
              suppliers={suppliers}
              onChanged={() => queryClient.invalidateQueries({ queryKey: ['admin-product-detail', slug] })}
            />
          ))}
        </tbody>
      </table>

      <div className="mt-3 flex flex-wrap items-end gap-2">
        <input
          placeholder="Couleur"
          value={newVariant.color}
          onChange={(e) => setNewVariant((v) => ({ ...v, color: e.target.value }))}
          className="w-24 rounded border border-black/20 px-2 py-1 text-xs"
        />
        <input
          placeholder="Taille"
          value={newVariant.size}
          onChange={(e) => setNewVariant((v) => ({ ...v, size: e.target.value }))}
          className="w-16 rounded border border-black/20 px-2 py-1 text-xs"
        />
        <input
          placeholder="SKU"
          value={newVariant.sku}
          onChange={(e) => setNewVariant((v) => ({ ...v, sku: e.target.value }))}
          className="w-32 rounded border border-black/20 px-2 py-1 text-xs"
        />
        <input
          type="number"
          step="0.01"
          placeholder="Prix d'achat"
          value={newVariant.costPrice}
          onChange={(e) => setNewVariant((v) => ({ ...v, costPrice: e.target.value }))}
          className="w-24 rounded border border-black/20 px-2 py-1 text-xs"
        />
        <input
          type="number"
          placeholder="Stock"
          value={newVariant.initialQuantity}
          onChange={(e) => setNewVariant((v) => ({ ...v, initialQuantity: Number(e.target.value) }))}
          className="w-20 rounded border border-black/20 px-2 py-1 text-xs"
        />
        <button
          type="button"
          disabled={addVariant.isPending || !newVariant.color || !newVariant.size || !newVariant.sku}
          onClick={() => addVariant.mutate()}
          className="rounded-full border border-luna-black px-3 py-1 text-xs disabled:opacity-40"
        >
          + Ajouter une variante
        </button>
      </div>
      {addVariantError && <p className="mt-1 text-xs text-red-600">{addVariantError}</p>}
    </div>
  );
}
