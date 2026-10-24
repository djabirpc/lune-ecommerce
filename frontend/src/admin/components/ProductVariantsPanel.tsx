import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { catalogApi } from '../../lib/api/catalog';
import { suppliersApi } from '../../lib/api/suppliers';
import { ApiError } from '../../lib/api/client';
import { formatPrice } from '../../lib/format/price';
import type { ProductVariantDto, SupplierDto } from '../../lib/api/types';
import { RestockAdjustControls } from './RestockAdjustControls';

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
        <RestockAdjustControls variantId={variant.id} suppliers={suppliers} onChanged={onChanged} />
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
            <th className="px-3 py-1">Actions</th>
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
